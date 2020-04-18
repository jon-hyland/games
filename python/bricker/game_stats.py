"""Bricker - A Tetris-like brick game.
Copyright (C) 2017-2020  John Hyland
GNU GENERAL PUBLIC LICENSE Version 3"""

from typing import List
import sys
import os.path


class GameStats:
    """Stores current score, high scores, and other game statistics."""

    def __init__(self) -> None:
        """Class constructor."""
        self.__high_scores: List[HighScore] = self.__load_high_scores()
        self.__current_score: int = 0
        self.__lines: int = 0
        self.__level: int = 1

    @property
    def high_scores(self) -> List['HighScore']:
        """Returns list of high scores."""
        return self.__high_scores

    @property
    def current_score(self) -> int:
        """Returns the current score."""
        return self.__current_score

    @property
    def lines(self) -> int:
        """Returns the number of lines cleares."""
        return self.__lines

    @property
    def level(self) -> int:
        """Returns the current level."""
        return self.__level

    @level.setter
    def level(self, value: int) -> None:
        """Sets the current level."""
        self.__level = value

    def __load_high_scores(self) -> List['HighScore']:
        """Load high scores from file."""
        scores = []
        if os.path.isfile("high_scores.txt"):
            with open("high_scores.txt", "r") as f:
                lines = f.readlines()
            lines = [x.strip() for x in lines]
            for line in lines:
                split = line.split("\t")
                if len(split) == 2:
                    initials = split[0]
                    score = int(split[1])
                    scores.append(HighScore(initials, score))
            scores = self.__sort_scores(scores)
        return scores

    def __save_high_scores(self, scores: List['HighScore']) -> None:
        """Save high scores to file."""
        scores = self.__sort_scores(scores)
        with open("high_scores.txt", "w") as text_file:
            for x in scores:
                text_file.write(x.initials + "\t" + str(x.score) + "\n")

    def is_high_score(self) -> bool:
        """Returns true if score can be placed on board."""
        if len(self.__high_scores) < 10:
            return True
        lowest = sys.maxsize
        for score in self.__high_scores:
            if score.score < lowest:
                lowest = score.score
        return self.__current_score > lowest

    def add_high_score(self, initials: str) -> None:
        """Adds new score, sorts and limits to top 10, saves to disk."""
        self.__high_scores.append(HighScore(initials, self.__current_score))
        self.__high_scores = self.__sort_scores(self.__high_scores)
        self.__save_high_scores(self.__high_scores)

    @staticmethod
    def __sort_scores(scores: List['HighScore']) -> List['HighScore']:
        """Sorts scores and returns new list.  Truncates to top ten."""
        scores.sort(key=lambda x: x.score, reverse=True)
        while len(scores) > 10:
            del scores[-1]
        return scores

    def add_lines(self, count: int) -> None:
        """Increments cleared lines, sets level."""
        self.__lines += count
        self.__level = (self.__lines // 20) + 1

    def increment_score(self, value: int) -> None:
        """Increments current score by specified value."""
        self.__current_score += value


class HighScore:
    """Stores a single high score."""

    def __init__(self, initials: str, score: int) -> None:
        """Class constructor."""
        self.__initials: str = initials
        self.__score: int = score

    @property
    def initials(self) -> str:
        """Returns gamer's initials."""
        return self.__initials

    @property
    def score(self) -> int:
        """Returns high-score value."""
        return self.__score
