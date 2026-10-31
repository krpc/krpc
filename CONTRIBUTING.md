# Contributing

Contributions to kRPC are welcome, from a typo fix to a new service.

## Reporting Bugs and Requesting Features

Open an [issue](https://github.com/krpc/krpc/issues/new/choose) and pick the template that fits.
Search the existing issues first, and add to one that already covers it.

A good bug report includes a script or save file that reproduces the bug, the kRPC and game
versions, and any other mods installed. A good feature request includes an example of the code you
would like to write.

Ask questions on [Discord](https://discord.gg/bXuaTrj).

## Making a Change

Small changes, such as bug fixes and documentation fixes, can go straight to a pull request. For a
larger feature or an API change, open an issue first so we can agree on the design before you write
the code.

1. Fork the repository and create a branch.
2. Install [bazelisk](https://github.com/bazelbuild/bazelisk). The build is hermetic and downloads
   everything else it needs. See [Building from
   Source](https://krpc.github.io/krpc/dev/krpc-development/building.html).
3. Make your change. [Code Structure](https://krpc.github.io/krpc/dev/krpc-development/code-structure.html)
   explains where things live.
4. Run the tests for the part you changed, e.g. `bazel test //client/python:test`. See
   [Testing](https://krpc.github.io/krpc/dev/krpc-development/testing.html). The service tests need
   a copy of KSP.
5. Open a pull request that says what it changes and why, and links the issue it fixes. Tick "Allow
   edits by maintainers".

CI runs the full test suite on every pull request. Draft pull requests are welcome if you want
early feedback.

A few things make a pull request easier to review:

 * One change per pull request.
 * Leave unrelated code as it is, including its style.
 * Add a test if you can. If you are not sure how, ask on the pull request and we can help.
 * Document a new or changed RPC in the XML comments of its C# source.

You do not need to tidy your commits. Before merging, we may push small fixes to your branch,
such as a missing changelog entry.

## License

Your contribution is licensed under the license of the part of kRPC you change. See
[LICENSE](LICENSE).
