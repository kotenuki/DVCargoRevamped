#!/bin/bash
current=$(pwd)
cd "/mnt/d/Programming/GitHub Repos/DVCargoMod/DVCargoMod"
cp "bin/Release/net47/DVCargoMod.dll" "DVCargoMod"
zip -r "DVCargoMod.zip" "DVCargoMod"
cd $current
git add .
git commit
git push