# TE13 - Operator mail check

Gustavo needs one deterministic way to prove that a real message is delivered, once the sender domain is set up at the provider. Add a module command (operator commands are `IModuleCommand`) that sends one rendered test email through `IMailService` to an address typed by the operator, prints only the result category, and never logs the address. No automated run sends real mail; this command is run by a person. Spec: row TE13 in `Docs/project.md`.
