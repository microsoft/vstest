#!/usr/bin/env bash

set -e

source "scripts/build.sh" "$@"

if [[ $? -ne 0 ]]; then
	exit 1
fi
