using UnityEngine;
public class BugFixer : MonoBehaviour
{
    private bool hasBugs = true;

    void Update()
    {
        if (hasBugs)
        {
            FixAllBugs();
        }
    }

    void FixAllBugs()
    {
        hasBugs = false;
    }
}