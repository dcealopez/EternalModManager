using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace EternalModManager
{
    /// <summary>
    /// Online safety
    /// </summary>
    public static class OnlineSafety
    {
        /// <summary>
        /// Online-safe mod name keywords
        /// </summary>
        public static string[] OnlineSafeModNameKeywords = new string[]
        {
            "/eternalmod/",
            ".tga",
            ".png",
            ".swf",
            ".bimage",
            "/advancedscreenviewshake/",
            "/audiolog/",
            "/audiologstory/",
            "/automap/",
            "/automapplayerprofile/",
            "/automapproperties/",
            "/automapsoundprofile/",
            "/env/",
            "/font/",
            "/fontfx/",
            "/fx/",
            "/gameitem/",
            "/globalfonttable/",
            "/gorebehavior/",
            "/gorecontainer/",
            "/gorewounds/",
            "/handsbobcycle/",
            "/highlightlos/",
            "/highlights/",
            "/hitconfirmationsoundsinfo/",
            "/hud/",
            "/hudelement/",
            "/lightrig/",
            "/lodgroup/",
            "/material2/",
            "/md6def/",
            "/modelasset/",
            "/particle/",
            "/particlestage/",
            "/renderlayerdefinition/",
            "/renderparm/",
            "/renderparmmeta/",
            "/renderprogflag/",
            "/ribbon2/",
            "/rumble/",
            "/soundevent/",
            "/soundpack/",
            "/soundrtpc/",
            "/soundstate/",
            "/soundswitch/",
            "/speaker/",
            "/staticimage/",
            "/swfresources/",
            "/uianchor/",
            "/uicolor/",
            "/weaponreticle/",
            "/weaponreticleswfinfo/",
            "/entitydef/light/",
            "/entitydef/fx",
            "/impacteffect/",
            "/uiweapon/",
            "/globalinitialwarehouse/",
            "/globalshell/",
            "/warehouseitem/",
            "/warehouseofflinecontainer/",
            "/tooltip/",
            "/livetile/",
            "/tutorialevent/",
            "maps/game/dlc/",
            "maps/game/dlc2/",
            "maps/game/horde/",
            "maps/game/hub/",
            "maps/game/shell/",
            "maps/game/sp/",
            "maps/game/tutorials/",
            "/decls/campaign/"
        };

        /// <summary>
        /// Online-unsafe resource name keywords
        /// </summary>
        public static string[] UnsafeResourceNameKeywords = new string[]
        {
            "gameresources",
            "generated", // Old mods compatibility.
            "pvp",
            "shell",
            "warehouse"
        };

        /// <summary>
        /// Determines whether or not a mod is safe for online play
        /// </summary>
        /// <param name="mod">mod zip archive</param>
        /// <returns>whether or not the mod is safe for online play</returns>
        public static bool IsModSafeForOnline(ZipArchive mod)
        {
            bool isSafe = true;
            bool isModifyingUnsafeResource = false;
            List<ZipArchiveEntry> assetsInfoJsons = new List<ZipArchiveEntry>();

            foreach (var modFile in mod.Entries)
            {
                // Skip directories
                if (string.IsNullOrEmpty(modFile.Name) && modFile.FullName.EndsWith("/"))
                {
                    continue;
                }

                // Skip files at the root
                if (!modFile.FullName.Contains("/"))
                {
                    continue;
                }

                var modFileEntry = modFile.FullName;
                var containerName = modFileEntry.Split('/')[0];
                var modName = modFileEntry.Substring(containerName.Length + 1);

                // Allow sound mods
                if (Directory.Exists(Path.Combine(App.GameFolder, "base", "sound", "soundbanks", "pc", $"{containerName}.snd")))
                {
                    continue;
                }

                // Check assets info files last
                if (modFileEntry.StartsWith("EternalMod/assetsinfo/", StringComparison.OrdinalIgnoreCase)
                    && modFileEntry.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    assetsInfoJsons.Add(modFile);
                    continue;
                }

                if (UnsafeResourceNameKeywords.Any(keyword => containerName.StartsWith(keyword)))
                {
                    isModifyingUnsafeResource = true;
                }

                // Files with .lwo extension are unsafe - also catches $ variants such as .lwo$uvlayout_lightmap=1
                if (Path.GetExtension(modName).Contains(".lwo"))
                {
                    isSafe = false;
                }

                // Allow modification of anything outside of "generated/decls/", except .entities files
                if ((!modName.StartsWith("generated/decls/", StringComparison.OrdinalIgnoreCase) &&
                    !modName.StartsWith("decls/", StringComparison.OrdinalIgnoreCase)) &&
                    !modName.EndsWith(".entities", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (isSafe)
                {
                    isSafe = OnlineSafeModNameKeywords.Any(keyword => modName.ToLower().Contains(keyword));
                }
            }

            if (isSafe)
            {
                return true;
            }

            if (!isSafe && isModifyingUnsafeResource)
            {
                return false;
            }

            // Don't allow adding unsafe mods in safe resource files into unsafe resources files
            // Otherwise, don't mark the mod as unsafe, it should be fine for single-player if
            // the mod is not modifying a critical resource
            foreach (var assetsInfoFile in assetsInfoJsons)
            {
                var resourceName = assetsInfoFile.FullName.Split('/')[0];
                var zipFileHandle = assetsInfoFile.Open();
                var buffer = new byte[assetsInfoFile.Length];
                zipFileHandle.Read(buffer, 0, buffer.Length);

                var assetsInfo = AssetsInfo.FromJson(Encoding.UTF8.GetString(buffer));

                if (assetsInfo != null)
                {
                    if (assetsInfo.Resources != null
                        && UnsafeResourceNameKeywords.Any(keyword => resourceName.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}