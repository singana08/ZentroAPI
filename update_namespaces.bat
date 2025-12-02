@echo off
echo Updating namespace references from HaluluAPI to ZentroAPI...

powershell -Command "(Get-Content 'Controllers\*.cs' -Raw) -replace 'using HaluluAPI\.', 'using ZentroAPI.' | Set-Content 'Controllers\*.cs'"
powershell -Command "(Get-Content 'Controllers\*.cs' -Raw) -replace 'namespace HaluluAPI\.', 'namespace ZentroAPI.' | Set-Content 'Controllers\*.cs'"

powershell -Command "(Get-Content 'DTOs\*.cs' -Raw) -replace 'using HaluluAPI\.', 'using ZentroAPI.' | Set-Content 'DTOs\*.cs'"
powershell -Command "(Get-Content 'DTOs\*.cs' -Raw) -replace 'namespace HaluluAPI\.', 'namespace ZentroAPI.' | Set-Content 'DTOs\*.cs'"

powershell -Command "(Get-Content 'Services\*.cs' -Raw) -replace 'using HaluluAPI\.', 'using ZentroAPI.' | Set-Content 'Services\*.cs'"
powershell -Command "(Get-Content 'Services\*.cs' -Raw) -replace 'namespace HaluluAPI\.', 'namespace ZentroAPI.' | Set-Content 'Services\*.cs'"

powershell -Command "(Get-Content 'Utilities\*.cs' -Raw) -replace 'using HaluluAPI\.', 'using ZentroAPI.' | Set-Content 'Utilities\*.cs'"
powershell -Command "(Get-Content 'Utilities\*.cs' -Raw) -replace 'namespace HaluluAPI\.', 'namespace ZentroAPI.' | Set-Content 'Utilities\*.cs'"

powershell -Command "(Get-Content 'Hubs\*.cs' -Raw) -replace 'using HaluluAPI\.', 'using ZentroAPI.' | Set-Content 'Hubs\*.cs'"
powershell -Command "(Get-Content 'Hubs\*.cs' -Raw) -replace 'namespace HaluluAPI\.', 'namespace ZentroAPI.' | Set-Content 'Hubs\*.cs'"

powershell -Command "(Get-Content 'Middleware\*.cs' -Raw) -replace 'using HaluluAPI\.', 'using ZentroAPI.' | Set-Content 'Middleware\*.cs'"
powershell -Command "(Get-Content 'Middleware\*.cs' -Raw) -replace 'namespace HaluluAPI\.', 'namespace ZentroAPI.' | Set-Content 'Middleware\*.cs'"

powershell -Command "(Get-Content 'TempModels\*.cs' -Raw) -replace 'namespace HaluluAPI\.', 'namespace ZentroAPI.' | Set-Content 'TempModels\*.cs'"

powershell -Command "(Get-Content 'Data\*.cs' -Raw) -replace 'using HaluluAPI\.', 'using ZentroAPI.' | Set-Content 'Data\*.cs'"

echo Done!