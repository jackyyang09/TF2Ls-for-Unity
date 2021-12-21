# Overview
TF2Ls for Unity (or TF2 Libraries/Loaders for Unity) is a collection of editor tools that help streamline the porting and usage of Valve assets in the development of Unity games.

This set of tools is a collection of existing scripts from past community developers packed together and given a very friendly user interface. Things that required too many mouse clicks, minutes, and headaches can now be done in seconds!

# What's Included
TF2 Shaders for the Built-in Render Pipeline that support paint and translucency

![](https://github.com/jackyyang09/TF2Ls-for-Unity/blob/Media/Media/Shader%20Paint%20Demo.png)

A mesh skeleton switcher that allows for the easy parenting of item/cosmetic/weapon rigs to character skeletal meshes

![](https://raw.githubusercontent.com/jackyyang09/TF2Ls-for-Unity/Media/Media/Skeleton%20Swapper%20Demo.png)

A model texturing tool that automatically reads .VMT files, generates new material assets, converts associated .VTFs into .TGAs and finally applies them to a mesh. All in one press! 

![](https://raw.githubusercontent.com/jackyyang09/TF2Ls-for-Unity/Media/Media/Model%20Texture%20Demo.png)
*Video below*

https://user-images.githubusercontent.com/11392541/146690276-ef22c113-dfd8-42a0-9387-38e2b1108ac6.mp4

# Coming Soon
* A helper component that facilitates use of texture-based eye movement. Gizmos/handlers to control where irises are looking included
* Automatic .vpk ripping for specific assets by reading the local item schema

# Credits
* VTF import functionality is done in the backend using VTFLib [Neil "Jed" Jedrzejewski](https://developer.valvesoftware.com/wiki/User:Wunderboy) and [Ryan "Nemesis" Gregg](https://developer.valvesoftware.com/wiki/User:Nem) under the GPL and LGPL licenses.
* VMT parsing is done using scripts from [Frassle's Ibasa](https://github.com/Frassle/Ibasa) library under the MIT license.
* TF2 shader implementation by the contributions of various Unity forums forums. I use a [self-modified version that adds paint support](https://gist.github.com/jackyyang09/178c063d8eccad5b15de654977ff83df).
