using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp2.Models;

namespace WpfApp2.Data
{
    public class WorkspaceRepository
    {
        string dbPath = "app.db";
        public static void ConfigureBsonMapper()
        {
            var mapper = BsonMapper.Global;
            mapper.Entity<Workspace>()
                .Id(x => x.Path, false)
                .Ignore(x => x.WsFolder)
                .Ignore(x => x.OutputFolder);

        }
        public WorkspaceRepository() { }

        public Workspace? GetLastRecentWorkspace()
        {
            using (var db = new LiteDatabase(dbPath))
            {
                var col = db.GetCollection<Workspace>("workspace");
                return col.FindAll().OrderByDescending(x => x.LastVisitedTime).FirstOrDefault();
            }
        }

        public List<Workspace> GetRecentWorkspaces()
        {
            using (var db = new LiteDatabase(dbPath))
            {
                var col = db.GetCollection<Workspace>("workspace");
                return col.FindAll().OrderByDescending(x => x.LastVisitedTime).ToList();
            }
        }

        public Workspace Get(string workspacePath)
        {
            using (var db = new LiteDatabase(dbPath))
            {
                var col = db.GetCollection<Workspace>("workspace");
                return col.FindById(workspacePath);
            }
        }
        public Workspace Insert(Workspace workspace)
        {
            // Open database (or create if doesn't exist)
            using (var db = new LiteDatabase(dbPath))
            {
                // Get customer collection
                var col = db.GetCollection<Workspace>("workspace");

                col.Insert(workspace);
                return workspace;
            }
        }

        public Workspace Update(Workspace workspace)
        {
            using (var db = new LiteDatabase(dbPath))
            {
                var col = db.GetCollection<Workspace>("workspace");
                col.Update(workspace);
                return workspace;
            }
        }

        public bool Delete(string workspacePath)
        {
            using (var db = new LiteDatabase(dbPath))
            {
                var col = db.GetCollection<Workspace>("workspace");
                return col.Delete(workspacePath);
            }
        }
    }
}
