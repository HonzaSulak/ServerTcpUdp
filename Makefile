# Makefile for building ServerTcpUdp.sln using dotnet build

# Variables
SOLUTION = ServerTcpUdp.sln
SRC = ServerTcpUdp
LINK = chat-server


# Default target
all: clean src
# Build target
src:
	dotnet build $(SRC)
	ln -s ./$(SRC)/bin/Debug/net8.0/$(SRC) ./$(LINK)

run:
	./$(LINK)

# Clean target
clean:
	dotnet clean $(SOLUTION)
	rm -f $(LINK)