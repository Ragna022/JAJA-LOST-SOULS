using System.IO;
using UnityEngine;

public class SaveFileDataWriter : MonoBehaviour
{
    public string saveDataDirectoryPath = "";
    public string saveFileName = "";

    // BEFORE WE SAVE A NEW FILE, MUST CHECK  IF ONE OF THIS CHARACTER SLOT ALRAEDY EXISTS (MAX 10 CHARACTER SLOTS)
    public bool CheckToSeeIfFileExist()
    {
        if (File.Exists(Path.Combine(saveDataDirectoryPath, saveFileName)))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void DeleteSaveFile()
    {
        File.Delete(Path.Combine(saveDataDirectoryPath, saveFileName));
    }

}
