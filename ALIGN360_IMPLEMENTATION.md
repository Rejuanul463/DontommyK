# Dontomyk native UI rebuild

The original screen-space Canvas in SampleScene is disabled. Its hierarchy and existing scene edits are retained. The new UI is constructed by DontomykUI.cs using Unity Canvas, Image, Text, Button, Slider, InputField and ScrollRect components. The world-space clock Canvas remains enabled.

Press Play in SampleScene. The rebuilt splash opens the main menu. Journal, Stats and Settings use the bottom navigation. Start Day opens live workplace choices. ALIGN360 ASSESSMENT opens the assessment workflow implemented previously.

## Screens and connections

- Splash and main menu reuse the supplied title and icon assets over the office environment.
- Gameplay choices add a dated journal entry and recorded activity. Time spent in an active workplace session is saved as practice time. Those choices do not alter assessment scores.
- Stats includes live activity totals, week/month/year chart controls, development practice counts and assessment result links.
- Journal includes a date selector, scrolling entries, favorites, reflection editing and saving. Its paper and spiral binding are generated UI elements; stored content remains editable text.
- Settings saves audio levels, vibration, display preferences, text size, accent color, week start, practice time and tips. Music and SFX levels target looping and non-looping scene audio sources respectively.
- Progress reset requires confirmation. Sign Out returns to the title and retains local progress because no online account is configured.

The reference is the supplied Figma file, excluding image node 36-161. The screens were recreated from the publicly visible frames and existing project assets. Original Figma font metadata and editable asset exports were not available; bundled Apache-licensed Droid Serif fonts provide the serif heading and journal styles. The result is a native reconstruction, not a verified pixel-for-pixel export of the Figma file.

Device notifications, a store listing, online authentication, and final client legal documents remain unconfigured. Their controls explain the current local behavior. No Android or iOS release package was built or tested on hardware.

## Verification

Runtime code compiles against the installed Unity 6000.3.25f1 assemblies. The isolated Play mode smoke test passed assessment submission, pre/post comparison, draft resume, both journal save paths, exercise completion, scenario responses, native screen navigation and workplace activity persistence. It uses temporary data and restores the user's save store. The native screens were inspected in the iPhone 12 simulator.

For review while playing, use Tools > Dontomyk > Preview. Tools > Dontomyk > Capture Game View saves a rendered preview to Unity's temporary cache.
