#!/bin/bash
# Find files containing a byte order mark, and remove it
sed -i '1 s/^\xef\xbb\xbf//' `grep -rlI $'\xEF\xBB\xBF'`
