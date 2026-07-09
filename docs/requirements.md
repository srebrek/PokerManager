# Poker Manager

## Terminology
- Hand: Single "Mini Game" eg. ends after River
- Game: Whole meeting. Consists of multiple Hands

## Problem
During the friendly poker (Texas Holdem) meetings it is hard to keep up with the Pot size,
the player stack size how to settle the game financially.

## Solution
The Poker Manager application keeps track of the current game state and after the game shows
what money transfers should be made. Additionally offers statistics for those who enter their cards.

## Requirements
### Basic Functional
- Player uses his smartphone
- Player does not have to register
- One of the Players is a Host and can act for others
- Player does not have to use the app (those players are managed by the Host)
- Player view is minimalistic: Player stack, pot size, buttons coloured after each chip, for each chip bet Player clicks
coresponding button and confirms with the confirm button
- Host can undo actions if any human errors occur
- Host view is like each player but also can switch to the other Players view
- Host can enter cards flop, turn and river
- each player can enter their cards after each Hand
- Playes can start a Game (becomes a host)
- Player can join the game through a code

## Aditional Functional
- Player sees his statistics after the game
- Player sees more statistics if he enters his cards after hands
- Player can register to save his statistics

## Technical
- Blazor WASM
- Modular Monolith
- .NET 10
- Aspire
- Postgres
- DDD
- EDA
- Sliced Architecture
- Integration Tests
- Mudblazor
- SignalR for game state (SSE (majority of incoming events and only one outgoing confirm that can be done through rest)
or websockets)
- Game state is saved as a log to be able to make statistics
