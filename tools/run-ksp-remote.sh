#!/bin/bash
set -e
tools/install.sh
DISPLAY=:0 $KSP_DIR/KSP.x86 &
trap 'kill $(jobs -p)' EXIT
tail -f "$HOME/.config/unity3d/Squad/Kerbal Space Program/Player.log"
