# Kryptert lærerveiledning

`index.html` dekrypterer lærerfasiten lokalt i nettleseren. Den krypterte teksten ligger i `lærerfasit.enc.js`; verken passord eller dekryptert innhold lagres av siden.

## Bruk

1. Åpne `index.html` i en moderne nettleser.
2. Skriv inn passordet du har fått separat.
3. Velg **Åpne lærerveiledningen**.
4. Velg **Lås** når du vil fjerne den dekrypterte teksten fra siden.

Siden gjør ingen nettverkskall og bruker ikke `localStorage`, cookies eller annen persistent lagring. Passordet finnes kortvarig i nettleserens minne mens nøkkelen avledes.

## Sikkerhetsmodell

Innholdet er kryptert med AES-256-GCM. Nøkkelen avledes fra passordet med PBKDF2-SHA-256, tilfeldig salt og 600 000 iterasjoner. AES-GCM kontrollerer også at innholdet ikke er endret.

En som kopierer filene kan forsøke passordgjetting lokalt uten å kontakte denne siden. Et langt og unikt passord er derfor avgjørende. Kryptering kan ikke gjøre et kort eller kjent passord sterkt.

## Kryptere en oppdatert fasit

`Encrypt-TeacherGuide.ps1` spør interaktivt etter passord. Det finnes med vilje ingen parameter for å sende passordet på kommandolinjen.

```powershell
.\Encrypt-TeacherGuide.ps1 `
  -InputPath .\midlertidig-lærerfasit.html `
  -DeleteSource
```

`-DeleteSource` sletter klartekstfilen etter vellykket kryptering. Ta ønsket sikkerhetskopi før du bruker valget. Output skrives til `lærerfasit.enc.js` dersom `-OutputPath` ikke oppgis.
