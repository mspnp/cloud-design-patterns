#!/bin/bash
set -e

CD=$PWD

cd ./azure-function/bin/Debug/netcoreapp2.1/publish/

rm sample-2-bin.zip --force

zip -r sample-2-bin.zip .

cd $CD

rm ./sample-2-bin.zip --force

mv ./azure-function/bin/Debug/netcoreapp2.1/publish/sample-2-bin.zip ./
