# Fix .Net Start failed silent
   1. Fixed Encoding: Re-saved appsettings.Development.json with explicit UTF-8 encoding.
   2. Process Cleanup: Terminated all invalid/orphaned dotnet processes.
      Get-Process dotnet | Stop-Process -Force
   3. Clean Build: Deleted bin and obj folders to ensure no stale artifacts.
      Remove-Item -Recurse -Force bin, obj

# If you encounter Failed to bind to address, it means a previous "silent" instance is still running.
   Find blocking process:
      Get-NetTCPConnection -LocalPort 5000, 5166
   Kill process:
      Stop-Process -Id <PID> -Force

# Verify
   dotnet run -v detailed
   
   http://localhost:5166/swagger/index.html
