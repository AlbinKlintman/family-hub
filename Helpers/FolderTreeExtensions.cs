using WebApp.Models;

namespace WebApp.Helpers;

public static class FolderTreeExtensions
{
    /// <summary>
    /// Flattens a folder list into parent-before-children order with a depth
    /// for each entry, so it can drive an indented dropdown.
    /// </summary>
    public static List<(Folder Folder, int Depth)> FlattenOrdered(this IEnumerable<Folder> folders)
    {
        var all = folders.ToList();
        var byParent = all.ToLookup(f => f.ParentFolderId);
        var result = new List<(Folder, int)>();

        void Walk(int? parentId, int depth)
        {
            foreach (var folder in byParent[parentId].OrderBy(f => f.Name))
            {
                result.Add((folder, depth));
                Walk(folder.Id, depth + 1);
            }
        }

        Walk(null, 0);
        return result;
    }

    /// <summary>
    /// True if <paramref name="candidateAncestorId"/> is <paramref name="folderId"/> itself
    /// or one of its descendants -- i.e. setting it as the parent would create a cycle.
    /// </summary>
    public static bool WouldCreateCycle(this IEnumerable<Folder> folders, int folderId, int candidateAncestorId)
    {
        if (folderId == candidateAncestorId)
        {
            return true;
        }

        var all = folders.ToList();
        var byParent = all.ToLookup(f => f.ParentFolderId);

        bool IsDescendant(int id)
        {
            foreach (var child in byParent[id])
            {
                if (child.Id == candidateAncestorId || IsDescendant(child.Id))
                {
                    return true;
                }
            }
            return false;
        }

        return IsDescendant(folderId);
    }
}
