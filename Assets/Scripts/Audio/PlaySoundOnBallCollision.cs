using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySoundOnBallCollision : MonoBehaviour
{
    [SerializeField] private AudioClip[] SFX_BallCollision;
    private void OnCollisionEnter(Collision collision)
    {
        //SoundManager.Instance.PlaySoundEffect(SFX_BallCollision);
        if (collision.gameObject.CompareTag("striped") || collision.gameObject.CompareTag("filled") || collision.gameObject.CompareTag("white") || collision.gameObject.CompareTag("black"))
        {
            //Debug.Log(Mathf.Clamp(collision.relativeVelocity.magnitude / 10, 0.01f, 1f));
            float collisionSpeed = collision.relativeVelocity.magnitude;
            int soundIndex = Mathf.Clamp(Mathf.RoundToInt(collisionSpeed / 10), 0, SFX_BallCollision.Length - 1);
            SoundManager.Instance.SetPitch(Random.Range(0.9f, 1.1f));
            float volume = Mathf.Clamp(collisionSpeed / 10, 0.01f, 1f);
            SoundManager.Instance.PlaySoundEffect(SFX_BallCollision[soundIndex], volume);
            return;
        }
    }
}
