using System;
using System.IO;
using OpenBveApi;
using OpenBveApi.Graphics;
using OpenBveApi.Hosts;
using OpenBveApi.Sounds;
using OpenBveApi.Textures;
using OpenBveApi.Trains;
using SoundManager;

namespace TrainEditor2.Systems
{
	/// <summary>Represents the host application.</summary>
	internal class Host : HostInterface
	{
		public Host() : base(HostApplication.TrainEditor2) { }

		// --- texture ---

		public override bool LoadTexture(ref Texture Texture, OpenGlTextureWrapMode wrapMode)
		{
			return Program.Renderer.TextureManager.LoadTexture(ref Texture, wrapMode, Environment.TickCount, InterpolationMode.BilinearMipmapped, 16);
		}

		// --- sound ---

		/// <summary>Loads a sound and returns the sound data.</summary>
		/// <param name="path">The path to the file or folder that contains the sound.</param>
		/// <param name="sound">Receives the sound.</param>
		/// <returns>Whether loading the sound was successful.</returns>
		public override bool LoadSound(string path, out Sound sound)
		{
			if (string.IsNullOrEmpty(path))
			{
				sound = null;
				return false;
			}

			if (File.Exists(path) || Directory.Exists(path))
			{
				foreach (ContentLoadingPlugin plugin in Program.CurrentHost.Plugins)
				{
					if (plugin.Sound != null)
					{
						try
						{
							if (plugin.Sound.CanLoadSound(path))
							{
								try
								{
									if (plugin.Sound.LoadSound(path, out sound))
									{
										return true;
									}
								}
								catch (ArgumentException ex)
								{
									// Invalid path or argument passed to sound loader
									System.Diagnostics.Debug.WriteLine($"Failed to load sound '{path}': {ex.Message}");
								}
								catch (IOException ex)
								{
									// File access error
									System.Diagnostics.Debug.WriteLine($"Failed to load sound '{path}': {ex.Message}");
								}
								catch (NotSupportedException ex)
								{
									// Unsupported sound format
									System.Diagnostics.Debug.WriteLine($"Failed to load sound '{path}': {ex.Message}");
								}
								catch (InvalidOperationException ex)
								{
									// Plugin in invalid state
									System.Diagnostics.Debug.WriteLine($"Failed to load sound '{path}': {ex.Message}");
								}
							}
						}
						catch (InvalidOperationException ex)
						{
							// Plugin collection modified during iteration
							System.Diagnostics.Debug.WriteLine($"Plugin error while loading sound '{path}': {ex.Message}");
						}
						catch (NullReferenceException ex)
						{
							// Plugin not properly initialized
							System.Diagnostics.Debug.WriteLine($"Plugin error while loading sound '{path}': {ex.Message}");
						}
					}
				}
			}

			sound = null;
			return false;
		}

		public override object PlaySound(SoundHandle buffer, double pitch, double volume, OpenBveApi.Math.Vector3 position, object parent, bool looped)
		{
			return Program.SoundApi.PlaySound(buffer, pitch, volume, position, parent, looped);
		}

		public override void StopSound(object SoundSource)
		{
			Program.SoundApi.StopSound(SoundSource as SoundSource);
		}

		public override AbstractTrain ParseTrackFollowingObject(string objectPath, string tfoFile)
		{
			throw new NotImplementedException();
		}
	}
}
