# YS-Mouse-Movement-Tracker

Hi! 👋 This is my first repository!  
**YS-Mouse-Movement-Tracker** is a lightweight desktop application built in C# using WPF that allows you to **record**, **play back**, and **simulate** mouse movements and clicks — including scrolls and right-clicks.

---

## 🖱️ Features

- 🟢 Record cursor position in real-time  
- 🔴 Capture left-clicks, right-clicks, and scroll wheel movements  
- 🔁 Playback with accurate timing and smooth motion  
- 💾 Saves session data to a `.txt` file in the same directory as the `.exe` file  
- ✅ Bypasses many synthetic cursor detection systems  

---

## 📁 How It Works

The app uses low-level Windows APIs (`user32.dll`) to track and simulate native mouse input:

- `GetCursorPos` and `SetCursorPos` for position tracking  
- `mouse_event` for simulating mouse actions  
- A low-level mouse hook to capture scroll activity  

All movements are timestamped and saved in this format:

```
time\_seconds\:X:Y\:event\:data
```

---

## 🧪 Example Recorded Events

```
0.0000:100:150\:move:
0.0201:100:150\:left\_down:
0.0356:100:150\:left\_up:
0.1204:100:150\:wheel:120
0.4500:200:300\:right\_down:
0.4600:200:300\:right\_up:
```

📂 Or check out sample trace files in the repository.

---

## 🚀 How to Use

### 🧱 Using the raw code

1. Clone or download the repository  
2. Open the solution in **Visual Studio**  
3. Build and run the app  
4. Use the buttons:
   - `Record` – Start recording mouse events  
   - `Stop` – Stop and save the recording  
   - `Playback` – Replay everything in order  

📝 Your session will be saved as a `mouse_trace.txt` file in the same folder as the `.exe`.

### 📦 Using the finished `.exe` file

1. Download the `.exe` file  
2. Run it  
3. Use the buttons:
   - `Record` – Start recording mouse events  
   - `Stop` – Stop and save the recording  
   - `Playback` – Replay everything in order  

📝 The `mouse_trace.txt` file will be generated in the same directory.

### 📝 Things to note
1. The `.exe` would read any file called `mouse_trace.txt` in the same directory when user presses the `Playback` button. You can put the example files there and rename it to `mouse_trace.txt` to see how they work
2. The `.exe` file will write/overwrite any file called `mouse_trace.txt` so be careful with the file names
3. The `.exe` has no keyboard shortcuts, so avoid recording/writing a `mouse_trace.txt` file that overrides all hardware mouse inputs. Developer will **not be responsible** for any damage done caused by this tool.

---

## 🛠️ Requirements

- Windows OS  
- .NET Framework (or .NET Core) with WPF support  
- Visual Studio 2019+ recommended (for building from source)

---

## 🙏 Credits

- Created by **Yinnotayl**  
- Initially built to auto-scroll through Roblox **Grow A Garden**’s seed shop 🌱

---

## 📌 Notes

- This tool is for **educational and testing purposes only**.  
- Use responsibly. Avoid using it in any apps or games **where automation violates terms of service**.  
- The developer is **not responsible** for any misuse or rule violations caused by this tool.

---

🎉 Thank you for checking out my first project!
