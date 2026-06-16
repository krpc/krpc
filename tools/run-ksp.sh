#!/bin/bash
set -e
tools/install.sh
(cd "${KSP_DIR:-lib/ksp}"; ./KSP.x86_64) &
trap 'kill $(jobs -p)' EXIT
sleep 3
tail -f "$KSP_DIR/KSP.log" | grep -i krpc
