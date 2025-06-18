using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "ProfileImageDataBase", menuName = "Background/ProfileImageDataBase", order = 1)]
public class ProfileImageDataBase : ScriptableObject
{
    [SerializeField] private ProfileImage[] profileImages;

    private Dictionary<string, ProfileImage> lookup;

    private void OnEnable()
    {
        if (lookup == null)
        {
            lookup = new Dictionary<string, ProfileImage>();
            foreach (var img in profileImages)
            {
                if (!lookup.ContainsKey(img.profileImageName))
                    lookup.Add(img.profileImageName, img);
            }
        }
    }

    public ProfileImage GetProfileImage(int index)
    {
        return profileImages[index];
    }

    public ProfileImage GetProfileImageByName(string profileName)
    {
        if (lookup == null) OnEnable();

        if (lookup.TryGetValue(profileName, out var image))
        {
            return image;
        }

        Debug.LogWarning("❌ Profile image not found: " + profileName);
        return null;
    }

    public int GetProfilesCount() => profileImages.Length;
}