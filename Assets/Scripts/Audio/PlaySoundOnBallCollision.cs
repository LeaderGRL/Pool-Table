using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySoundOnBallCollision : MonoBehaviour
{
    [SerializeField] private AudioClip SFX_BallCollision;
    private void OnCollisionEnter(Collision collision)
    {
        //SoundManager.Instance.PlaySoundEffect(SFX_BallCollision);
        if (collision.gameObject.CompareTag("striped") || collision.gameObject.CompareTag("filled") || collision.gameObject.CompareTag("white") || collision.gameObject.CompareTag("black"))
        {
            Debug.Log(collision.relativeVelocity.magnitude / 10);
            float volume = collision.relativeVelocity.magnitude / 10;
            SoundManager.Instance.PlaySoundEffect(SFX_BallCollision, volume);
            return;
        }
    }
}
