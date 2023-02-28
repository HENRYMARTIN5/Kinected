"""
Basic music player class using PyGame Mixer for the Kinected UDS
Path: /uds/music.py
"""

from betterlib.config import ConfigFile
import betterlib.logging, pygame.mixer, random, threading

class MusicPlayer:
    def __init__(self, logger : betterlib.logging.Logger, config : ConfigFile) -> None:
        pygame.mixer.init()
        self.logger = logger
        self.config = config
        self.music = []
        self.playing = False
        self.currentsong = None
        self.currentsongname = None
        self.currentsongindex = None
        self.currentsongthread = None
        self.reload_music()
        self.logger.info("[Music] Music player initialized")
    
    def reload_music(self) -> None:
        self.logger.info("[Music] Reloading music...")
        self.music = []
        for song in self.config.get("music"):
            self.music.append(song)
        self.logger.info("[Music] Music reloaded")

    def play(self, songname : str = None) -> None:
        if self.playing:
            self.stop()
        if songname is None:
            songname = random.choice(self.music)
        self.logger.info("[Music] Playing song: " + songname)

        self.currentsong = pygame.mixer.Sound(songname)
        self.currentsongname = songname
        self.currentsongindex = self.music.index(songname)
        self.currentsongthread = threading.Thread(target=self.currentsong.play)
        self.currentsongthread.start()
        self.playing = True  
    
    def stop(self) -> None:
        if self.playing:
            self.logger.info("[Music] Stopping song: " + self.currentsongname)
            self.currentsong.stop()
            self.currentsong = None
            self.currentsongname = None
            self.currentsongindex = None
            self.currentsongthread = None
            self.playing = False
    
    def next(self) -> None:
        currentsongindex = self.currentsongindex # Values get overwritten on stop()
        if self.playing:
            self.stop()
        
        if currentsongindex == len(self.music) - 1:
            self.play(self.music[0])
        else:
            self.play(self.music[currentsongindex + 1])
    
    def previous(self) -> None:
        if self.playing:
            self.stop()
        if self.currentsongindex == 0:
            self.play(self.music[len(self.music) - 1])
        else:
            self.play(self.music[self.currentsongindex - 1])
    
    def is_playing(self) -> bool:
        return self.playing
    
    def get_current_song(self) -> str:
        return self.currentsongname

    def toggle(self) -> None:
        if self.playing:
            self.stop()
        else:
            self.play()