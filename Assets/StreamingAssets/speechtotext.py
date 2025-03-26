import whisper_timestamped as whisper
import os
import glob
import json


def main():
    model = whisper.load_model('medium.en', device="cpu")

    file_pattern = os.path.join("Separated", "*_Vocals*.wav")

    matching_files = glob.glob(file_pattern)

    if matching_files:
        # Return the first matching file
        first_matching_file = matching_files[0]

        print("Found:", first_matching_file)

        audio = whisper.load_audio(first_matching_file)

        result = whisper.transcribe(model, audio, language="en")

        parsed_data = json.loads(json.dumps(result, indent=2, ensure_ascii=False))

        output_lines = [f"{word['start']} {word['text']} {word['end']}" for segment in parsed_data['segments'] for word
                        in segment['words']]

        sentence_lines = [f"{segment['start']} {segment['text']} {segment['end']}" for segment in
                          parsed_data['segments']]

        with open(os.path.join("Separated", "lyrics.txt"), "w", encoding='utf-8') as output_file:
            output_file.write('\n'.join(output_lines))

        with open(os.path.join("Separated", "sentences.txt"), "w", encoding='utf-8') as output_file1:
            output_file1.write('\n'.join(sentence_lines))

        print("Transcript saved.")


def add_timestamps(segment):
    timestamps = [i / 100 for i in range(int(segment['start'] * 100), int(segment['end'] * 100))]
    timestamps = [f"{t:.2f}" for t in timestamps]  # Format timestamps with 2 decimal places
    return timestamps


if __name__ == '__main__':
    main()
