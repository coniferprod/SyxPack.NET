echo $SYXPACK_VERSION
echo $SYXPACK_CONFIGURATION
echo $LOCAL_NUGET_PATH
dotnet build && dotnet pack && nuget add SyxPack/bin/$SYXPACK_CONFIGURATION/SyxPack.$SYXPACK_VERSION.nupkg -source $LOCAL_NUGET_PATH
