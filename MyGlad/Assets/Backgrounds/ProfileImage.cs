using UnityEngine;

[CreateAssetMenu(fileName = "ProfileImage", menuName = "Backgrounds/ProfileImage", order = 2)]
public class ProfileImage : ScriptableObject
{
    public string profileImageName; // Name of the item
    public Sprite profileImage; // Icon for the item (for the UI)
}