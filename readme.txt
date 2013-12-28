Thanks for using CERNSSO!

Windows Phone 8 coding? There is an issue!

HtmlAgilityPack doesn't properly install.
You'll have to manually add the HtmlAgilityPack.dll to your project.
Do this by adding it from the sl3-wp folder in the packages directory of
your solution (for standard NuGet usage) Thanks!

If you are referencing this in a Portable Class Library (PCL) then you *must* reference
it in the platform specific library that calls your PCL. If you do not you'll
get an exception complaining that the platform specific version of the library has not
been referenced.