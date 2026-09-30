![This is ChroMapper.](https://i.imgur.com/nQ7caC2.png)

[![Crowdin](https://badges.crowdin.net/chromapper/localized.svg)](https://crowdin.com/project/chromapper)

# This is ChroMapper.
ChroMapper is a Unity-based map editor for Beat Saber, specializing in modded map creation. ChroMapper also offers various tools and features that tailor towards the advanced mappers of the community.

## Features
- **360/90 Support** for the first time ever in a community map editor.
- **Chroma 2.0 Lighting** brings lightshows to the next level.
- **Optimized** to run on a wide range of devices, from high-end VR machines to [Chromebooks](https://cdn.discordapp.com/attachments/702231982335197264/892184054147993640/20210927_190030.jpg).
- **Cross-platform**, with official x64 builds available for Windows, Mac OS, and Linux. Individual users may also build ChroMapper for other platforms.
- **Rebindable Keys** means you can adapt your mapping workflow to whatever input device you have installed.

### Get Started
Check out the [ChroMapper Wiki](https://chromapper.atlassian.net/wiki/spaces/UG/overview) for some basic documentation about the program, setting it up, and its features.

For new users, you might also find the [ChroMapper Tutorial by Atlas Rhythm](https://youtu.be/6SixwKR43Zg) useful.

## GLS color distribution extensions

GLS color events and color event boxes support ordered `customData.shifts` and `customData.strobeShifts` arrays. Each compact string has the form `{targets},{signedOffset},{distributionEasing}`, for example `h,-0.2,ioqn`, `sv,0.3,lin`, or `f,0.1,iq`. Separate entries in the editor controls with semicolons.

Compact easing names are `lin`, `step`, or an `i`, `o`, or `io` prefix combined with `q` (quadratic), `c` (cubic), `qt` (quartic), `qn` (quintic), `s` (sinusoidal), `e` (exponential), `cr` (circular), `b` (back), `el` (elastic), or `bo` (bounce). Existing full Chroma easing names are also accepted.

Targets may contain `h`, `s`, `v`, `r`, `g`, `b`, and `f`. The first HSV/RGB character selects that instruction's color model, and later characters from the other model are ignored (`rs` changes only red; `sr` changes only saturation). Unknown target characters are ignored. `f` changes brightness/alpha independently and may be combined with either color model. Any finite signed offset is accepted; final component handling follows the existing color conversion/rendering path.

Box instructions apply to every event in that box, followed by that event's own instructions. `shifts` changes the normal endpoint and `strobeShifts` changes the strobe destination. A strobe without `strobeColor` derives that destination from the event's OEM or `customData.color` main color before applying `strobeShifts`.

Four spatial distribution interpretations were considered:

- A: interpolate across affected individual lights.
- B: interpolate across affected chunks, assigning one value to every light in a chunk.
- C: interpolate across every individual light while applying only to affected lights.
- D: interpolate across every chunk while applying only to affected chunks.

ChroMapper initially supports only mode B. Filtered-out chunks do not consume interpolation. The first affected chunk receives zero shift and the last receives the full offset; each instruction's easing controls only that spatial interpolation, while the event easing continues to control time.

For the eight-light example with four chunks, step 2, and hue offset `-0.7`, the affected lights receive `0, 0, [NA, NA], -0.7, -0.7, [NA, NA]`.

# Releases

## ChroMapper Launcher (Recommended)
The recommended method of installing ChroMapper is through the [ChroMapper Launcher](https://cm.topc.at/dl). Not only does this download the necessary files to run ChroMapper, but it also serves as an auto updater, so you never go out of date.

## Jenkins CI
Builds are automatically created with [Jenkins](https://jenkins.kirkstall.top-cat.me/view/All/job/ChroMapper/). Jenkins also pushes updates for the ChroMapper Launcher. If you need to download a specific version of ChroMapper, you can find them here.

## GitHub Releases
GitHub Releases will be made with every stable update. While builds will be distributed, it is recommended to use the ChroMapper Launcher to automatically stay up to date.

# Discord
The ChroMapper Discord is the central point of communication between users and the developers. It is recommended to visit the Discord to recieve support, talk about development, and share your work.

Join the Discord [here](https://discord.gg/YmEt9EZ8pw).

# Patreon
If you'd like to donate to the project and get some sweet perks, you can [support ChroMapper development on Patreon](https://www.patreon.com/Caeden117).

# Localization
If you want to help translate this application into other languages, you can head over to [CrowdIn](https://crwd.in/chromapper). Updated localization from CrowdIn is pulled into ChroMapper on a semi-occasional basis.

# For Developers
ChroMapper is developed with Unity in C#. Please see the [build guide](BUILD.md) for setting up your development environment.

## License
The project uses the [GNU GPL v2 license](https://github.com/Caeden117/ChroMapper/blob/master/LICENSE). Please keep that license in mind as you make contributions to ChroMapper.
