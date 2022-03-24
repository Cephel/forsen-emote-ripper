# forsen-emote-ripper

Downloads all emotes used by the twitch streamer ![forsen](https://www.twitch.tv/forsen).

This is the result of a fevered all nighter after our Discord was bumped to the maximum boost level and I had to find a way to fill the available emote slots with unfunny shit. 

This codebase is basically just a collection of antipatterns and "don't do this" and nobody should ever write code like this.

If you ask me if I regret spending easily 5 times the time it takes to just manually download all the emotes by hand to write this program, the answer is yes.

Features:
- Barely documented
- Pointless parallelism
- Lots of magic numbers
- Lack of code reuse all over the place
- Barely type safe
- Only green code paths tested
- Borderline irresponsible I/O