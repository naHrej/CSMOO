#!/bin/bash

echo "Starting CSMOO Server with .NET Hot Reload enabled..."
echo ""
echo "Hot Reload Features:"
echo "- Verb JSON files: Automatic reload on file changes"
echo "- Core C# code: Hot reload when you save files"
echo ""
echo "Press Ctrl+C to stop the server"
echo ""

# Set environment to Development to enable hot reload features
export DOTNET_ENVIRONMENT=Development

# Start with dotnet watch for automatic hot reload
dotnet watch run
