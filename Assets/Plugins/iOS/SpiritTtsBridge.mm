#import <AVFoundation/AVFoundation.h>

static AVSpeechSynthesizer *SpiritSynthesizer(void)
{
    static AVSpeechSynthesizer *synthesizer = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        synthesizer = [[AVSpeechSynthesizer alloc] init];
    });
    return synthesizer;
}

extern "C" void SpiritTtsSpeak(const char *text, float rate, float pitch)
{
    if (text == nullptr) return;
    NSString *phrase = [NSString stringWithUTF8String:text];
    dispatch_async(dispatch_get_main_queue(), ^{
        AVSpeechSynthesizer *synthesizer = SpiritSynthesizer();
        [synthesizer stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];

        AVSpeechUtterance *utterance = [AVSpeechUtterance speechUtteranceWithString:phrase];
        utterance.voice = [AVSpeechSynthesisVoice voiceWithLanguage:@"en-US"];
        utterance.rate = AVSpeechUtteranceDefaultSpeechRate * rate;
        utterance.pitchMultiplier = pitch;
        utterance.preUtteranceDelay = 0.04;
        [synthesizer speakUtterance:utterance];
    });
}

extern "C" void SpiritTtsStop(void)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        [SpiritSynthesizer() stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
    });
}
