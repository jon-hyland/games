"""Bricker - A Tetris-like brick game.
Copyright (C) 2017-2020  John Hyland
GNU GENERAL PUBLIC LICENSE Version 3"""

from typing import Tuple, List
from time import perf_counter
import pygame
from pygame import Surface
from pygame.time import Clock
from matrix import Matrix
from color import Colors
from renderer import Renderer
from game_stats import GameStats
from exploding_space import ExplodingSpace


class Bricker:
    """Contains main game logic and entry point."""

    def __init__(self) -> None:
        """Class constructor."""

        # load version
        try:
            with open("version", "r") as file:
                version = file.readline().strip()
        except Exception:
            version = "1.0"

        # init pygame framework
        pygame.init()

        # define class vars
        self.__screen_size: Tuple[int, int] = (1000, 700)
        self.__screen: Surface = pygame.display.set_mode(self.__screen_size)
        self.__clock: Clock = Clock()
        self.__renderer: Renderer = Renderer(version, self.__screen_size, self.__screen, self.__clock)
        self.__matrix: Matrix = Matrix()
        self.__stats: GameStats = GameStats()
        self.__level_drop_intervals: List[float] = []
        interval = 2.0
        for _ in range(0, 10):
            interval *= 0.8
            self.__level_drop_intervals.append(interval)


    def main(self) -> None:
        """Runs main game logic."""

        # vars
        in_game = False

        # program loop
        while True:

            # get menu selection
            menu_selection = self.menu_loop(in_game)

            # resume, run game loop
            if menu_selection == 1:
                in_game = self.game_loop()

            # start new game, run game loop
            elif menu_selection == 2:
                self.new_game()
                in_game = self.game_loop()

            # quit program
            elif menu_selection == 3:
                self.explode_spaces()
                break


    def menu_loop(self, in_game: bool) -> int:
        """The main menu loop."""

        # vars
        menu_selection = 1
        if not in_game:
            menu_selection = 2

        # loop until selection
        while True:

            # limit fps
            self.__clock.tick(60)

            # handle user events
            for event in pygame.event.get():

                # up
                if event.type == pygame.KEYDOWN and (event.key == pygame.K_LEFT or event.key == pygame.K_UP):
                    menu_selection -= 1
                    if in_game:
                        if menu_selection < 1:
                            menu_selection = 3
                    else:
                        if menu_selection < 2:
                            menu_selection = 3

                # down
                elif event.type == pygame.KEYDOWN and (event.key == pygame.K_RIGHT or event.key == pygame.K_DOWN):
                    menu_selection += 1
                    if in_game:
                        if menu_selection > 3:
                            menu_selection = 1
                    else:
                        if menu_selection > 3:
                            menu_selection = 2

                # enter
                elif event.type == pygame.KEYDOWN and (event.key == pygame.K_SPACE or event.key == pygame.K_RETURN):
                    return menu_selection

            # draw menu
            self.__renderer.draw_menu(self.__matrix, self.__stats, menu_selection, in_game)


    def high_score_loop(self) -> None:
        """The main menu loop."""

        # vars
        chars = list("   ")
        pos = 0
        numbers = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '0']
        letters = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z']
        done = False

        # loop
        while not done:

            # limit fps
            self.__clock.tick(60)

            # handle user events
            for event in pygame.event.get():
                if event.type == pygame.KEYDOWN:
                    if str(pygame.key.name(event.key)) in letters + numbers:
                        if pos < 3:
                            char = str(pygame.key.name(event.key))
                            chars[pos] = char
                            pos += 1
                    elif event.key == pygame.K_BACKSPACE:
                        pos -= 1
                        if pos < 0:
                            pos = 0
                        if pos <= 2:
                            chars[pos] = " "
                    elif event.key == pygame.K_RETURN:
                        done = True

            # draw frame
            self.__renderer.draw_initials_input(self.__matrix, self.__stats, chars)

        # add new high score
        initials = "".join(chars).lower()
        self.__stats.add_high_score(initials)


    def game_loop(self) -> bool:
        """The main game loop.  Returns true if still in game (menu opened)."""

        # vars
        game_over = False

        # event loop
        while not game_over:

            # reset hit flag
            hit = False

            # limit fps
            self.__clock.tick(60)

            # handle user events
            for event in pygame.event.get():

                # left
                if event.type == pygame.KEYDOWN and event.key == pygame.K_LEFT:
                    self.move_brick_left()

                # right
                elif event.type == pygame.KEYDOWN and event.key == pygame.K_RIGHT:
                    self.move_brick_right()

                # down
                elif event.type == pygame.KEYDOWN and event.key == pygame.K_DOWN:
                    self.move_brick_down()

                # rotate
                elif event.type == pygame.KEYDOWN and event.key == pygame.K_UP:
                    self.rotate_brick()

                # drop
                elif event.type == pygame.KEYDOWN and event.key == pygame.K_SPACE:
                    self.drop_brick_to_bottom()
                    hit = True

                # menu
                elif event.type == pygame.KEYDOWN and (event.key == pygame.K_ESCAPE or event.key == pygame.K_q):
                    return True

                # level up
                elif event.type == pygame.KEYDOWN and event.key == pygame.K_PAGEUP:
                    if self.__renderer.debug:
                        self.__stats.level += 1
                        if self.__stats.level > 10:
                            self.__stats.level = 10

                # level down
                elif event.type == pygame.KEYDOWN and event.key == pygame.K_PAGEDOWN:
                    if self.__renderer.debug:
                        self.__stats.level -= 1
                        if self.__stats.level < 1:
                            self.__stats.level = 1

                # debug toggle
                elif event.type == pygame.KEYDOWN and event.key == pygame.K_d:
                    self.__renderer.debug = not self.__renderer.debug

            # drop brick timer?
            if self.is_drop_time():    # add drop interval
                hit = self.move_brick_down()

            # brick hit bottom?
            if hit:
                game_over = self.brick_hit()

            # draw frame
            self.__renderer.update_frame(self.__matrix, self.__stats, None)

        # game over
        self.explode_spaces()
        if self.__stats.is_high_score():
            self.high_score_loop()
        return False


    def new_game(self) -> None:
        """Resets state and starts a new game."""
        self.__stats = GameStats()
        self.__matrix.new_game()


    def move_brick_left(self) -> None:
        """Moves brick left."""
        self.__matrix.move_brick_left()


    def move_brick_right(self) -> None:
        """Moves brick right."""
        self.__matrix.move_brick_right()


    def move_brick_down(self) -> bool:
        """Moves brick down.  Returns true if brick hits bottom."""
        hit = self.__matrix.move_brick_down()
        if hit:
            self.__stats.increment_score(1)
        return hit


    def rotate_brick(self) -> None:
        """Rotates brick."""
        self.__matrix.rotate_brick()


    def drop_brick_to_bottom(self) -> None:
        """Animates a brick dropping to bottom of screen."""
        hit = False
        while not hit:
            self.__renderer.clock.tick(30)
            for _ in range(0, 3):
                hit = self.move_brick_down()
                if hit:
                    break
            self.__renderer.event_pump()
            self.__renderer.update_frame(self.__matrix, self.__stats, None)
        self.__stats.increment_score(2)


    def is_drop_time(self) -> bool:
        """Returns true if it's time for brick to drop."""
        if self.__matrix.brick is not None:
            drop_interval = self.__level_drop_intervals[self.__stats.level - 1]
            return self.__matrix.brick.is_drop_time(drop_interval)
        return False


    def brick_hit(self) -> bool:
        """Executed when brick hits bottom and comes to rest.  Spawns new brick.  Returns true on new brick collision (game over)."""
        self.__matrix.add_brick_to_matrix()
        rows_to_erase = self.__matrix.identify_solid_rows()
        if len(rows_to_erase) > 0:
            rows = len(rows_to_erase)
            self.__stats.add_lines(rows)
            points = 40
            if rows == 2:
                points = 100
            elif rows == 3:
                points = 300
            elif rows == 4:
                points = 1200
            self.__stats.increment_score(points)
            self.erase_filled_rows(rows_to_erase)
            self.drop_grid()
            self.__renderer.event_pump()
            self.__renderer.update_frame(self.__matrix, self.__stats, None)
        collision = self.__matrix.spawn_brick()
        return collision


    def erase_filled_rows(self, rows_to_erase: List[int]) -> None:
        """Animates erasure of filled rows."""
        for x in range(1, 11):
            for y in rows_to_erase:
                self.__matrix.matrix[x][y] = 0
                self.__matrix.color[x][y] = Colors.Black
            if (x % 2) == 0:
                self.__renderer.event_pump()
                self.__renderer.update_frame(self.__matrix, self.__stats, None)


    def drop_grid(self) -> None:
        """Drops hanging pieces to resting place."""
        while self.drop_grid_once():
            pass


    def drop_grid_once(self) -> bool:
        """Drops hanging pieces, bottom-most row."""
        top_filled_row = 0
        for row in range(1, 21):
            empty = True
            for x in range(1, 11):
                if self.__matrix.matrix[x][row] == 1:
                    empty = False
                    break
            if not empty:
                top_filled_row = row
                break
        if top_filled_row == 0:
            return False
        bottom_empty_row = 0
        for row in range(20, (top_filled_row - 1), -1):
            empty = True
            for x in range(1, 11):
                if self.__matrix.matrix[x][row] == 1:
                    empty = False
                    break
            if empty:
                bottom_empty_row = row
                break
        if bottom_empty_row == 0:
            return False
        for y in range(bottom_empty_row, 1, -1):
            for x in range(1, 11):
                self.__matrix.matrix[x][y] = self.__matrix.matrix[x][y - 1]
                self.__matrix.color[x][y] = self.__matrix.color[x][y - 1]
        for x in range(1, 11):
            self.__matrix.matrix[x][1] = 0
            self.__matrix.color[x][1] = Colors.Black
        return True


    def explode_spaces(self) -> None:
        """Explodes matrix spaces outwards on game over."""
        self.__matrix.add_brick_to_matrix()
        spaces: List[ExplodingSpace] = []
        for x in range(1, 11):
            for y in range(1, 21):
                if self.__matrix.matrix[x][y] == 1:
                    space_x = (((x - 1) * 33) + 2) + ((self.__renderer.screen_size[0] - 333) // 2) - 1
                    space_y = (((y - 1) * 33) + 2) + ((self.__renderer.screen_size[1] - 663) // 2) - 1
                    spaces.append(ExplodingSpace(space_x, space_y, self.__matrix.color[x][y]))
                    self.__matrix.matrix[x][y] = 0
                    self.__matrix.color[x][y] = Colors.Black
        start_time = perf_counter()
        have_spaces = True
        while have_spaces:
            seconds = perf_counter() - start_time
            have_spaces = False
            for space in spaces:
                space.x += space.x_motion * seconds
                space.y += space.y_motion * seconds
                if (space.x > 0) and (space.x < 1000) and (space.y > 0) and (space.y < 700):
                    have_spaces = True
            self.__clock.tick(30)
            self.__renderer.update_frame(self.__matrix, self.__stats, spaces)


# start main function
if __name__ == "__main__":
    bricker = Bricker()
    bricker.main()
