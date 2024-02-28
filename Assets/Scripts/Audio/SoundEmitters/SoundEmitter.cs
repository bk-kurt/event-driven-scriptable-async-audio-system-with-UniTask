using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class SoundEmitter : MonoBehaviour
{
    private AudioSource _audioSource;

    private readonly Queue<(AudioClip clip, AudioConfigurationSO settings, bool hasToLoop, Vector3 position)> 
        _playbackQueue = new();
    private CancellationTokenSource _cancellationTokenSource;
    public event UnityAction<SoundEmitter> OnSoundFinishedPlaying;

    private void Awake()
    {
        _audioSource = this.GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
    }

    /// <summary>
    /// Instructs the AudioSource to play a single clip, with optional looping, in a position in 3D space.
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="settings"></param>
    /// <param name="hasToLoop"></param>
    /// <param name="position"></param>
    public async UniTask PlayAudioClipInOrder(AudioClip clip, AudioConfigurationSO settings, bool hasToLoop, Vector3 position = default)
    {
        // Cancel any ongoing playback sequence
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Enqueue the new clip
        _playbackQueue.Enqueue((clip, settings, hasToLoop, position));
        
        // If not currently playing anything, start the playback sequence
        if (!_audioSource.isPlaying)
        {
            await PlayQueuedClipsAsync(_cancellationTokenSource.Token);
        }
    }

    private async UniTask PlayQueuedClipsAsync(CancellationToken cancellationToken)
    {
        while (_playbackQueue.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            var (clip, settings, hasToLoop, position) = _playbackQueue.Dequeue();
            _audioSource.clip = clip;
            settings.ApplyTo(_audioSource);
            _audioSource.transform.position = position;
            _audioSource.loop = hasToLoop;
            _audioSource.Play();
            
            if (!hasToLoop)
            {
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(clip.length), cancellationToken: cancellationToken);
                    OnSoundFinishedPlaying?.Invoke(this);
                }
                catch (OperationCanceledException)
                {
                    // Handle cancellation (e.g., clean up)
                    _audioSource.Stop();
                    break;
                }
            }
        }
    }

    public void CancelPlayback()
    {
        _cancellationTokenSource?.Cancel();
    }
    /// <summary>
    /// Used when the game is unpaused, to pick up SFX from where they left.
    /// </summary>
    public void Resume()
    {
        _audioSource.Play();
    }

    /// <summary>
    /// Used when the game is paused.
    /// </summary>
    public void Pause()
    {
        _audioSource.Pause();
    }

    /// <summary>
    /// Used when the SFX finished playing. Called by the <c>AudioManager</c>.
    /// </summary>
    public void Stop() // Redundant?
    {
        _audioSource.Stop();
    }

    public bool IsInUse()
    {
        return _audioSource.isPlaying;
    }

    public bool IsLooping()
    {
        return _audioSource.loop;
    }
}