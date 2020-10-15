Note: Tested with Windows 10 2004 using WSL 2

1) git clone https://github.com/kylewill0725/krpc to a folder in Windows. This folder from now on will be called `/krpc`
2) Install Ubuntu from Microsoft Store
3) Open Ubuntu
4) run the following to prevent conflicts with Windows
```
echo "[interop]
appendWindowsPath = false" | sudo tee -a /etc/wsl.conf
```
5) run `exit` and in a command prompt run `wsl --shutdown`.
6) Open Ubuntu
7) Install bazelisk by following this [link](https://docs.bazel.build/versions/3.6.0/install-bazelisk.html).
8) Add the mono repository from [here](https://www.mono-project.com/download/stable/#download-lin-ubuntu). Note: Windows Store Ubuntu is Ubuntu 20.04LTS unless other version is in name.
9) Run command (note: This is about 1.5GB download)
```
sudo apt-get install mono-complete python-setuptools python-virtualenv \
python-dev autoconf libtool luarocks texlive-latex-base \
texlive-latex-recommended texlive-fonts-recommended texlive-latex-extra \
libxml2-dev libxslt1-dev librsvg2-bin python3-dev python3-setuptools \
python3-virtualenv enchant latexmk openjdk-8-jdk
```
10) cd to `/krpc` in Ubuntu (ex. `/mnt/d/source/repos/krpc` but yours is probably different)
11) Using Windows command prompt, cd `/krpc/lib`
12) Run `mklink /D ksp {Kerbal_Space_Program_Directory}`
13) cd to `/krpc/lib/ksp`
14) Run `mklink /D KSP_Data .\KSP_x64_Data`
15) cd to `/krpc/lib` in Ubuntu.
16) Run `ln -s /usr/lib/mono/4.5 mono-4.5`
17) cd to `/krpc`
18) Build using `bazel build //:krpc`

## Special thanks to Tamer1an, Enroy, and jh0ker for helping me figure this all out.