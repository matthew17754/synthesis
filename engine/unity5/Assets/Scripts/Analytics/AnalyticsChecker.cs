using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;

/// <summary>
/// This will check to see if the analytics GameObject exists
/// inside the scene. If there isn't one then one will be created
/// </summary>
public class AnalyticsChecker : MonoBehaviour
{
    public GameObject analyticsManagerPrefab;

    public void Start()
    {
        if (GameObject.FindGameObjectWithTag("AM") == null)
        {
            Instantiate(analyticsManagerPrefab);
        }

        Destroy(gameObject);
    }
}
