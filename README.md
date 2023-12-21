# WPDtool
This tool allows you to unpack and repack the WPD IMGB files from the FINAL FANTASY XIII trilogy.

The program should be launched from command prompt with any one of these following argument switches along with the input file:
<br>``-u`` Unpacks the WPD file
<br>``-r`` Repacks the unpacked WPD folder to a WPD file

Commandline usage examples:
<br>``WPDtool.exe -u "c201.win32.trb" ``
<br>``WPDtool.exe -r "_c201.win32.trb" ``

Note: For the ``-r`` switch, the unpacked TRB folder name is specified in the example. the ``_`` in the name indicates the name of the unpacked folder.

### Important
- The Xbox 360 version image data is swizzled. due to this swizzled format, this tool will not unpack them correctly.
- Repacking is supported only for the PC version WPD IMGB files.

## For Developers
- This tool makes use of this following reference library:
<br>**IMGBlibrary** - https://github.com/Surihix/IMGBlibrary
- Refer to this [page](https://github.com/LR-Research-Team/Datalog/wiki/WPD-Pack-files) for information about the the WPD's file structure.
