#!/bin/bash
# Sourced by submit.sh and job.sh: the options that make every run use the deployed tool.
#
# The image's own copy of the tool (/opt/readability) is replaced at run time by app/publish,
# the copy deploy.ps1 uploads. So a code change only needs deploy.ps1 -SkipImageBuild, and the
# image is rebuilt only to change llama.cpp. This matters because image builds on the compute
# nodes can fail (fetching the base image from ghcr.io, or extracting an image without user
# namespaces), and because rebuilding takes the newest llama.cpp, which one experiment must
# not mix with an older one.
TOOL_BIND=(--bind "$ROOT/app/publish:/opt/readability")
