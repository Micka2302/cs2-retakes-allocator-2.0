# Build-time libraries

`Absynthium_MenuApi.dll` is the public API contract from the sibling
[`Absynthium_Menu`](https://github.com/Micka2302/Absynthium_Menu) project. It is referenced only to compile
RetakesAllocator and is not copied next to the plugin DLL. The release script copies
the deployable API and Core from `Absynthium_Menu/compiled`.

Absynthium_Menu is licensed under the MIT License.
