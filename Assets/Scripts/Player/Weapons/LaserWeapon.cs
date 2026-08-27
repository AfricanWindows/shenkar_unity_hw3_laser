using UnityEngine;

public class LaserWeapon  : MonoBehaviour,IWeapon
{
    [SerializeField] private GameObject projectile;

    public void Attack()
    {
        Debug.Log("Shoot Laser");
    }
}

