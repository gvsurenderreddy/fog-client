﻿/*
 * FOG Service : A computer management client for the FOG Project
 * Copyright (C) 2014-2015 FOG Project
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 3
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Zazzles;

namespace FOG
{
    public static class Helper
    {
        private const string LogName = "Installer";
        public const string ClientVersion = "0.11.3";

        public static IInstall Instance { get;  }

        static Helper()
        {
            switch (Settings.OS)
            {
                case Settings.OSType.Mac:
                    Instance = new MacInstall();
                    break;
                case Settings.OSType.Linux:
                    Instance = new LinuxInstall();
                    break;
                default:
                    Instance = new WindowsInstall();
                    break;
            }
        }

        public static bool PinServerCert(string location, bool preset = false)
        {
            return GenericSetup.PinServerCert(location, preset);
        }

        public static bool SaveSettings(string https, string usetray, string webaddress, string webroot,
            string company, string rootLog, string version = ClientVersion, string location = null)
        {
            return GenericSetup.SaveSettings(https, usetray, webaddress, webroot, company, 
                rootLog, version, location ?? Instance.GetLocation());
        }

        public static void ExtractFiles(string tmp, string location)
        {
            var tmpLocation = Path.Combine(tmp, "FOGService.zip");
            ExtractResource("FOG.Scripts.FOGService.zip", tmpLocation);
            ZipFile.ExtractToDirectory(tmpLocation, location);
            File.Delete(tmpLocation);
        }

        public static bool ExtractResource(string resource, string filePath, bool dos2Unix=false)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var input = assembly.GetManifestResourceStream(resource))
                using (var output = File.Create(filePath))
                {
                    CopyStream(input, output);
                }
                if(dos2Unix)
                    Dos2Unix(filePath);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not extract resource");
                Log.Error(LogName, ex.Message);
                return false;
            }

        }

        private static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8192];

            int bytesRead;
            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }

        public static bool PinServerCert()
        {
            return PinServerCert(null);
        }

        public static void PinFOGCert()
        {
            var certPath = Path.Combine(Instance.GetLocation(), "fog.ca.cer");

            ExtractResource("FOG.Scripts.fog-ca.cer", certPath , true);
            GenericSetup.InstallFOGCert(certPath);
        }

        public static void Dos2Unix(string fileName)
        {
            var fileData = File.ReadAllText(fileName);
            fileData = fileData.Replace("\r\n", "\n");
            File.WriteAllText(fileName, fileData);
        }

        public static void CreateRuntime()
        {
            var outputFile = Path.Combine(Instance.GetLocation(), "runtime");

            try
            {

                string[] stdout;
                ProcessHandler.Run("which", "mono-service", true, out stdout);
                if (stdout != null)
                {
                    var runtime = string.Join(" ", stdout).Trim();
                    var runtimeDir = Path.GetDirectoryName(runtime);
                    if (runtimeDir != null && !runtimeDir.EndsWith("/"))
                        runtimeDir = runtimeDir + "/";
                    File.WriteAllText(outputFile, runtimeDir);
                }
            }
            catch (Exception ex)
            {
                Log.Error(LogName, "Could not detect runtime");
                Log.Error(LogName, ex);
                File.Create(outputFile);
            }
        }
    }
}