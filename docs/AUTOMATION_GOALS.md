# Automation goals

Use these questions before choosing tools or writing more automated tests.

## Desired feedback

- Which feedback do we want from automation?
- Who needs the result, and how quickly?
- What decision should the result support?

## Risk and scope

- Which risks matter most?
- Which failures would have the greatest impact?
- Where do we currently have weak or missing evidence?

## Execution

- Which tests must be fast?
- What should run on every commit?
- What can run less often or on demand?
- What test data and environment are required?

## Boundaries

- What is still best tested manually?
- Which checks would be expensive or unreliable to automate?
- Where could lower-level tests provide better feedback than UI tests?

## Value

- How will we know that the automation provides value?
- What maintenance cost are we accepting?
- What signal would tell us to remove or redesign a test?
