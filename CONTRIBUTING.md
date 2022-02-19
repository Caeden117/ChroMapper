# ChroMapper Contributing Manual (CMCM)

Thanks for taking the time to contribute to ChroMapper.

This short manual will contain some useful information about creating the best contributions to ChroMapper.

## Important Resources

- [Join the Discord](https://discord.gg/YmEt9EZ8pw) to have a central line of communication between you and ChroMapper developers.
- [Read the BUILD guide](BUILD.md) to properly set up a ChroMapper dev environment.

# Good Bug Reports

A good bug report allows us to easily diagnose and solve the issue you are experiencing.

In ChroMapper, any errors and exceptions automatically open the Developer Console. You can toggle the Developer Console on and off with the `~` key.

Any bugs you should report are colored purple in the Developer Console, with a cyan flag icon appearing on the left. By clicking the flag, a bug report will be generated and uploaded to [paste.ee](https://paste.ee). This includes all of the relevant information we need to diagnose the issue. When the report is uploaded, ChroMapper will open the bug report in a new browser tab, allowing you to easily copy/paste the link.

## Reporting on Discord

You can report any bugs or issues in the `#bug-reports` channel. Please keep all bug reports in that channel.

Please be sure to include:
- ChroMapper version (Found in the bottom right of the Options menu)
- A clear and concise description of the problem
- If applicable, the paste.ee link to your generated bug report
- Applicable screenshots or videos
- Steps to reproduce the problem (if available)

## Reporting on GitHub

You can also report issues on the GitHub. Ensure that you selected the `ChroMapper Bug Report` issue template, and filling out the information that is requested.

# Pull Requests

Contributions to ChroMapper is always welcome, but to keep the project codebase consistent, we'd ask that you follow some guidelines when making pull requests.

## Coding Convention / Styling Guidelines

ChroMapper comes with a `.editorconfig` file which outlines most of the convention and styling guidelines used by the project.

A nice quite from [OpenGovernment's contributing file](https://github.com/opengovernment/opengovernment/blob/master/CONTRIBUTING.md):
```
This is open source software. Consider the people who will read your code, and make it look nice for them. It's sort of like driving a car: Perhaps you love doing donuts when you're alone, but with passengers the goal is to make the ride as smooth as possible.
```

When making pull requests, we ask that your PR **contains no errors or warnings from `.editorconfig` violations.** If not, a PR review will be sent asking to conform to the `.editorconfig` file, and your PR will not be merged until then.

### IDEs

The two main IDEs we use for ChroMapper development are Visual Studio and JetBrains Rider. These IDEs both support `.editorconfig` files, and will properly alert you to any violations.

If you use another IDE outside of Visual Studio or Rider, your mileage may vary.

## Cross-platform Compatibility

We ask that any and all changes to ChroMapper remain platform-agnostic to maintain ChroMapper's status as a cross-platform map editor.

If your pull request does contain platform-dependent code, we request that the code does not hinder ChroMapper's ability to run on other platforms.

Any pull requests that do not follow these cross-platform guidelines will not be merged.