#!/bin/bash
set -e
tools/install.sh
${KSP_DIR:-lib/ksp}/KSP.x86_64 &
trap 'kill $(jobs -p)' EXIT
sleep 3
tail -f "$HOME/.config/unity3d/Squad/Kerbal Space Program/Player.log"
