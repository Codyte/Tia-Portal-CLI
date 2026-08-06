@echo off
rem Shim de PATH: `tia <verbo>` de qualquer diretorio. Nunca chamar tia.exe direto --
rem tia.ps1/_common.ps1 roteia por sessao do Windows (sessao 0 nao attacha no Portal).
pwsh -NoProfile -File "%~dp0tia.ps1" %*
