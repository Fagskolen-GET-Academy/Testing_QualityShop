[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$InputPath,

    [string]$OutputPath = (Join-Path $PSScriptRoot 'lærerfasit.enc.js'),

    [switch]$DeleteSource
)

$iterationCount = 600000
$salt = [byte[]]::new(16)
$nonce = [byte[]]::new(12)
$tag = [byte[]]::new(16)
$key = $null
$passwordBytes = $null
$plainBytes = $null
$cipherBytes = $null
$combinedBytes = $null
$passwordPointer = [IntPtr]::Zero
$securePassword = Read-Host 'Passord for lærerfasiten' -AsSecureString

try {
    $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    if ([string]::IsNullOrWhiteSpace($password)) {
        throw 'Passordet kan ikke være tomt.'
    }

    $normalizedPassword = $password.Normalize([Text.NormalizationForm]::FormC)
    $passwordBytes = [Text.Encoding]::UTF8.GetBytes($normalizedPassword)
    $plainText = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $InputPath))
    $plainBytes = [Text.Encoding]::UTF8.GetBytes($plainText)

    [Security.Cryptography.RandomNumberGenerator]::Fill($salt)
    [Security.Cryptography.RandomNumberGenerator]::Fill($nonce)

    $derivation = [Security.Cryptography.Rfc2898DeriveBytes]::new(
        $passwordBytes,
        $salt,
        $iterationCount,
        [Security.Cryptography.HashAlgorithmName]::SHA256)
    try {
        $key = $derivation.GetBytes(32)
    }
    finally {
        $derivation.Dispose()
    }

    $cipherBytes = [byte[]]::new($plainBytes.Length)
    $associatedDataText = 'Testing_QualityShop teacher guide v1'
    $associatedData = [Text.Encoding]::UTF8.GetBytes($associatedDataText)
    $aes = [Security.Cryptography.AesGcm]::new($key, $tag.Length)
    try {
        $aes.Encrypt($nonce, $plainBytes, $cipherBytes, $tag, $associatedData)
    }
    finally {
        $aes.Dispose()
    }

    $combinedBytes = [byte[]]::new($cipherBytes.Length + $tag.Length)
    [Array]::Copy($cipherBytes, 0, $combinedBytes, 0, $cipherBytes.Length)
    [Array]::Copy($tag, 0, $combinedBytes, $cipherBytes.Length, $tag.Length)

    $payload = [ordered]@{
        version = 1
        algorithm = 'AES-GCM'
        kdf = [ordered]@{
            name = 'PBKDF2'
            hash = 'SHA-256'
            iterations = $iterationCount
        }
        salt = [Convert]::ToBase64String($salt)
        iv = [Convert]::ToBase64String($nonce)
        tagLength = 128
        aad = $associatedDataText
        ciphertext = [Convert]::ToBase64String($combinedBytes)
    }

    $json = $payload | ConvertTo-Json -Depth 4 -Compress
    $outputText = "window.ENCRYPTED_TEACHER_GUIDE = $json;"
    $resolvedOutputPath = [IO.Path]::GetFullPath($OutputPath)
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($resolvedOutputPath)) | Out-Null
    [IO.File]::WriteAllText($resolvedOutputPath, $outputText, [Text.UTF8Encoding]::new($false))

    if ($DeleteSource) {
        $resolvedInputPath = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $InputPath))
        if ($resolvedInputPath -eq $resolvedOutputPath) {
            throw 'Input- og outputfil kan ikke være samme fil.'
        }

        Remove-Item -LiteralPath $resolvedInputPath
    }

    Write-Host "Kryptert lærerveiledning skrevet til $resolvedOutputPath"
}
finally {
    if ($passwordPointer -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    }

    foreach ($buffer in @($passwordBytes, $plainBytes, $key, $cipherBytes, $combinedBytes)) {
        if ($null -ne $buffer) {
            [Array]::Clear($buffer, 0, $buffer.Length)
        }
    }

    $securePassword.Dispose()
    $password = $null
    $normalizedPassword = $null
    $plainText = $null
}
