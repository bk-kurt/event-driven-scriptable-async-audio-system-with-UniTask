# event-driven-scriptable-async-audio-system-with-UniTask
An improvement to an event driven scriptable Audio Management system using UniTask

- About UniTask dependency: https://github.com/Cysharp/UniTask
- About working with Event Driven Scriptable Architectures : https://github.com/UnityTechnologies/open-project-1

 ### UniTask Utilization

Basically we are replacing the coroutine-based approach. This change can reduce overhead and improve performance.

Also bringing, 
- Support for Cancellation
- Queue Management
- Ordered Playback


 ```C#
public async UniTask PlayAudioClipInOrder(AudioClip clip, AudioConfigurationSO settings, bool hasToLoop, Vector3 position = default)
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        
        _playbackQueue.Enqueue((clip, settings, hasToLoop, position));
        
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
                    _audioSource.Stop();
                    break;
                }
            }
        }
    }
 ```

### Fire and forget approach via UniTask

UniTaskVoid Return Type: The PlayAudioClip method is now an asynchronous method with a UniTaskVoid return type, which is suitable for fire-and-forget scenarios common in game development.


We could track and maanage tasks (UniTasks) instead of forgetting them, however thats not that crucial for current audio management scenario. <br>

 ```C#
public void PlayAudioCue(AudioCueSO audioCue, AudioConfigurationSO settings, Vector3 position = default)
	{
		AudioClip[] clipsToPlay = audioCue.GetClips();
		int nOfClips = clipsToPlay.Length;

		for (int i = 0; i < nOfClips; i++)
		{
			SoundEmitter soundEmitter = _pool.Request();
			if (soundEmitter != null)
			{
				// Wrap the call in the safe fire-and-forget method
				SafeFireAndForget(soundEmitter.PlayAudioClipInOrder(clipsToPlay[i], settings, audioCue.looping, position));
                
				if (!audioCue.looping)
					soundEmitter.OnSoundFinishedPlaying += OnSoundEmitterFinishedPlaying;
			}
		}
	}
```

### About safe-fire-and-forget 

- Ideal for independently running tasks that don't require completion monitoring, such as sound effects, logging, or analytics events.
- Simplifies code by eliminating task lifecycle management, perfect for non-critical tasks.

### However

- With the help of UniTask, I believe we can design better and performant systems for task lifecycle management and completion monitoring in further / other cases when needed.


