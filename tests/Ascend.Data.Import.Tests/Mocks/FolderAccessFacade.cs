using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ascend.Data.Import.Tests.Mocks
{
    public class FolderAccessFacade : IDataAccessFacade<string>
    {
        public Task<bool> FileExistAsync(string path)
        {
            return Task.FromResult(File.Exists(path));
        }

        public Task<string> GetFileAsync(string path)
        {
            return Task.FromResult(path);
        }

        public Task<string[]> GetFilesAsync(string prefix)
        {
            return Task.FromResult(Directory.GetFiles("../../../../data"));
        }
    }
}
