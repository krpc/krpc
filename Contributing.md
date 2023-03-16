# Contributing

We welcome contributions to kRPC, but before doing so, please read this guide.

All contributions come under the respective licences detailed in the repository. For most
components, this is either the General Public License or the Lesser General Public License. See the
LICENSE file for more details.

The rest of this document is intended as a set of "best practices" or "guidelines", and does not
necessarily need to be followed exactly.

## Reporting Bugs

To report a bug, please create an issue on the GitHub issues page.

Please check that the issue has not already been reported. If one already exists, you can add to the
discussion there.

If an issue does not already exist, please create one. Include a clear description of what's wrong
and how to reproduce it. For example, an example script and/or save file that demonstrates the bug,
the version of KSP and kRPC used, and details of any other mods installed.

See below for more advice on how to write a good issue.

## Requesting Features

New features can be requested by creating an issue, with a description of what is wanted. Please
include as much detail as possible. For example, add an example of the code you would like to be
able to write.

As with bug reports, please first check that the feature has not already been requested.

See below for details about how to write a good issue.

## Where to get help

Other questions that aren't bugs or feature requests are best asked on the discord
server. Alternatively, you can create an issue and label it as a "question".

## Writing a Good Issue

Here are some general guidelines on writing a good issue:
 * Focus on one thing. Only report one bug, or request one feature. If you want to report multiple
   bugs/request multiple features, create one issue for each of them.
 * Check if an issue for the bug/feature already exists. Try to avoid creating duplicates.
 * If the issue is related to other issues, link to them.

## Workflow for Contributing

The following details a rough workflow from reporting a bug/issue, to writing code, and getting your
changes merged using a Pull Request.
 1) The first step to contributing is discussing the bug/feature on GitHub. Create an "issue" on the
    issues page if one does not already exist. Following the guidelines above for how to write a good
    issue. This provides a central place to discuss solutions/designs for the bug/feature before
    implementation work begins.
 1) After an issue is submitted, a member of the dev team will apply labels to it to
    categorise/prioritise the bug/feature. They may also assign it to a milestone if the issue needs
    to be resolved for a particular upcoming release.
 1) For larger features, make sure the design is discussed on the issue before you start writing
    code. Avoid the temptation to start coding immediately, it is better to start coding once you
    have a clear plan!
 1) Once a plan is in place, you can start implementing the bug fix/feature. See below for more
    details on writing code.
     * If you are an outside contributor, create a fork of the repository and make your changes on a
       branch in that fork.
     * If you are a kRPC dev team member with write access to the repository, create a branch in the
       repository directly (without forking). See below for details on best practices for
       branches. You should also "assign" the issue to yourself, to indicate that you are actively
       working on the issue.
 1) Once the implementation is complete, and all of your changes have been pushed to the branch,
    submit a Pull Request, to request that the changes be merged to the main branch.
     * Include a brief description of the changes in the Pull Request description.
     * Link the Pull Request to the relevant issue, if one exists. This allows us to match up the
       design discussions with the eventual implementation.
     * If you are an outside contributor, please check the “allow repository maintainer to make
       changes to this branch”. This allows members of the dev team to make small changes to your
       Pull Request if necessary.
 1) Once the Pull Request has been submitted, automated tests will be run using GitHub Actions to
    verify the changes do not break anything. These need to pass before the Pull Request can be
    merged.
 1) A member of the kRPC dev team will then review the changes in the Pull Request. They will provide
    constructive feedback, propose changes, and/or ask for clarification on parts that are not
    clear. This is an iterative process, and may take some time for larger Pull Requests. Once the
    reviewer is happy that the changes are ready to be merged they will approve the Pull Request.
 1) The Pull Request can then be merged into the main branch and the Pull Request marked as
    complete. If the related Issue is resolved by the Pull Request, that can also be marked as
    complete.
 1) Finally, the Pull Request and related issue should be added to the next release milestone, so
    that we can keep track of which changes are in the next release.

## Guidelines for Pull Requests

 * Focus on one thing. A pull request should only have code changes that fix/add one
   bug/feature. The exception to this is if there are many small bugs in one part of the code. These
   can be submitted in a single Pull Request.
 * Don’t fix code style in code that are not directly related to changes in your Pull Request. If
   you want to fix the other lines of code, do it in a separate Pull Request.
 * Keep a clean commit history and use meaningful commit messages. This makes the Pull Request
   easier to review. You can use git rebasing to clean up your commit history or bring in more
   recent changes from the main branch.
 * Keep your Pull Request up to date with the main branch. If there are merge conflicts, they will
   need to be resolved before the Pull Request can be merged.
 * Automated tests should pass. It is best to run the tests locally before submitting a Pull
   Requests to check that they are likely to pass.

## Git Repository Guidelines and Structure

Some general guidelines on using git and the structure of the repository:
 * The main branch is the latest "bleeding edge" version, and should always be fully compilable and
   functional.
 * The main branch is "protected". Changes can only be merged to it by following the Pull Request
   workflow detailed above.
 * Experimental changes or in-progress work should be done on a branch.
 * Branches should be named "<username>/<name>". For example "djungelorm/try-fixing-issue-435". This
   makes it clear who created the branch, to avoid conflicts.
 * Tags are created on the main branch for each release. These have the format "v?.?.?". The latest
   release is also tagged as "latest-release"
 * A special tag named "docs" indicates which commit is used to build the documentation
   website. This is a protected tag, and can only be modified by authorized users. When it is
   modified, GitHub actions automatically updates the documentation website.

## Writing Code

 * Details on how to set up a build environment and how to compile the project are detailed here:
   https://krpc.github.io/krpc/compiling.html
 * Write readable code that is "self documenting", and if not add comments to explain complex
   code. Remember that someone else may need to fix or improve upon your code in future, so they
   need to be able to understand it!
 * The coding style for the particular language/component you are working on should be
   followed. This helps make the code readable and more maintainable. The automated tests include
   some automated style checking.
    * TODO: need to specify the code style / formatting tools for each language/component. For now,
      just try and following the precedent set by existing code.
 * Each component has an associated CHANGES.txt file containing a list of changes and in which
   version they were made. When adding a feature/fixing a bug, you should add entries to the
   relevant CHANGES.txt file(s) indicating what was changed. This change log is intended for end
   users of the mod. If your changes are not relevant to an end user (for example, fixing a build
   script, or fixing some code style), you do not need to put an entry in the change log.
 * Document any new features/changes.
    * Documentation is written in ReStructured Text format and built using Sphinx.
    * The source code for this is in the `doc` directory.
    * Documentation for new/modified Remote Procedure Calls is written in XML comments in the C#
      code. This must be kept up to date, and new RPCs must have documentation comments added. The
      comments in the C# source code are used to auto-generate API documentation when the
      documentation is built.
 * Test, test, test! Manual testing is not sufficient. If you add a feature/fix a bug a unit test
   should be added that verifies that the feature works as expected or that the bug is fixed. This
   can then be included in the automated testing for all Pull Requests in future. How these tests
   are implemented is of course dependent on the component that is being modified.
    * Note: this advice does not apply to services, as these need to be tested while running the
      game, which is hard to automate. In this case, manual testing is sufficient.

## Further Resources

 * A high-level summary of how the kRPC server works: https://krpc.github.io/krpc/internals.html
 * How to write services for kRPC: https://krpc.github.io/krpc/extending.html
 * Documentation on the communication protocol:
   https://krpc.github.io/krpc/communication-protocols.html
