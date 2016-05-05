# Release Notes

##	 System Requirements

* Microsoft Windows 7 Service Pack 1, Microsoft Windows 8, Microsoft Windows 8.1, Microsoft Windows 10 or Microsoft Windows Server 2012 (32 bit or 64 bit editions)
* Microsoft .NET Framework version 4.5
* Microsoft Visual Studio 2015 Comunity, Enterprise, or Professional edition with Update 3 installed, or Visual Studio 2013 Ultimate, Premium, or Professional edition
* Windows Azure SDK for .NET version 2.9
* Windows Reactive Extensions Library

**NOTE:** Ensure that you have installed all applicable updates for your computer from Windows Update.

## Allow NuGet to Download Missing Packages

 
You must ensure that Visual Studio is configured to use the latest version of the NuGet package manager and allow it to download missing packages during the build process. To enable this:
1. Open the Visual Studio **Extentions and Updates** dialog from the **Tools** menu.
2. Check that the **NuGet Package Manager** is installed. If it is not already installed select **Online Gallery** and install it. You can use the Search feature to find it in the Gallery.
3. Open the Visual Studio **Options** dialog from the **Tools** menu.
4. Expand the section for **NuGet Package Manager** and select **General**.
5. Ensure that **Allow NuGet download missing package** and **Automatically check for missing packages during build in Visual Studio** are checked.
