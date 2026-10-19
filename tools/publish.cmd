@echo off
rem ---------------------------------------------------------------------
rem  SDSM (admin app) publish wrapper
rem    publish.cmd              build, publish, check and zip
rem    publish.cmd -CheckOnly   steps 1-3 only (no zip, no send folder)
rem
rem  ASCII ONLY. Do not put Korean text in this file.
rem  cmd.exe reads a batch file byte by byte using the OEM codepage; a
rem  UTF-8 file with multibyte characters makes it lose its read position
rem  and commands get truncated. All Korean lives in publish.ps1.
rem ---------------------------------------------------------------------
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish.ps1" %*
