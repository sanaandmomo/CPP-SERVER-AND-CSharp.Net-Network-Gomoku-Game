#define _CRT_SECURE_NO_WARNINGS
#include <windows.h>
#include <winsock.h>
#include <iostream>
#include <vector>
#include <sstream>
#pragma comment (lib, "ws2_32.lib")

using namespace std;

class Client {
private:
	int clientID;
	int roomID;
	SOCKET clientSocket;

public:
	Client(int clientID, SOCKET clientSocket) {
		this->clientID = clientID;
		this->roomID = -1;
		this->clientSocket = clientSocket;
	}

	int getClientID() {
		return this->clientID;
	}

	int getRoomID() {
		return this->roomID;
	}

	void setRoomID(int roomID) {
		this->roomID = roomID;
	}

	SOCKET getClientSocket() {
		return clientSocket;
	}
};

SOCKET serverSocket;
vector<Client> connections;
WSAData wsaData;
SOCKADDR_IN serverAddress;

int nextID;

// string�� split
vector<string> getTokens(string input, char delimieter) {
	vector<string> tokens;
	istringstream f(input);
	string s;

	while (getline(f, s, delimieter)) {
		tokens.push_back(s);
	}

	return tokens;
}

// �ش� �濡 �ִ� ������
int clientCountInRoom(int roomID) {
	int count = 0;

	for (int i = 0; i < connections.size(); i++) {
		if (connections[i].getRoomID() == roomID) {
			count++;
		}
	}

	return count;
}

void playClient(int roomID) {
	char *sent = new char[256];
	bool black = true;

	for (int i = 0; i < connections.size(); i++) {
		if (connections[i].getRoomID() == roomID) {
			ZeroMemory(sent, 256);

			if (black) {
				sprintf(sent, "%s", "[Play]Black");
				black = false;
			}
			else {
				sprintf(sent, "%s", "[Play]White");
			}

			send(connections[i].getClientSocket(), sent, 256, 0);
		}
	}
}

void exitClient(int roomID) {
	char *sent = new char[256];

	for (int i = 0; i < connections.size(); i++) {
		if (connections[i].getRoomID() == roomID) {
			ZeroMemory(sent, 256);
			sprintf(sent, "%s", "[Exit]");
			send(connections[i].getClientSocket(), sent, 256, 0);
		}
	}
}

void putClient(int roomID, int x, int y) {
	char *sent = new char[256];

	for (int i = 0; i < connections.size(); i++) {
		if (connections[i].getRoomID() == roomID) {
			ZeroMemory(sent, 256);
			string data = "[Put]" + to_string(x) + "," + to_string(y);
			sprintf(sent, "%s", data.c_str());
			send(connections[i].getClientSocket(), sent, 256, 0);
		}
	}
}

void ServerThread(Client *client) {
	char *sent = new char[256];
	char *received = new char[256];
	int size = 0;

	while (true) {
		if ((size = recv(client->getClientSocket(), received, 256, NULL)) == 0) {
			ZeroMemory(sent, 256);
			sprintf(sent, "Ŭ���̾�Ʈ [%i]�� ������ ���������ϴ�.", client->getClientID());
			cout << sent << endl;

			// ���ӿ��� ���� �÷��̾� ã��
			for (int i = 0; i < connections.size(); i++) {
				if (connections[i].getClientID() == client->getClientID()) {
					// �ٸ� ����ڿ� ���� ���̴� ����� ���� ���
					int roomID = connections[i].getRoomID();
					if (roomID != -1 && clientCountInRoom(roomID) == 2) {
						// �����ִ� ������� �޼��� ����
						exitClient(roomID);
					}

					connections.erase(connections.begin() + i);
					break;
				}
			}

			delete client;
			break;
		}

		string receivedString = string(received);
		vector<string> tokens = getTokens(receivedString, ']');

		if (receivedString.find("[Enter]") != -1) {
			string roomID = tokens[1];
			int roomInt = atoi(roomID.c_str());
			int clientCount = clientCountInRoom(roomInt);

			// �޼����� ���� Ŭ���̾�Ʈ�� ã��
			for (int i = 0; i < connections.size(); i++) {
				if (connections[i].getClientID() == client->getClientID()) {
					// ���� �� á�� ��
					if (clientCount >= 2) {
						ZeroMemory(sent, 256);
						sprintf(sent, "%s", "[Full]");
						send(client->getClientSocket(), sent, 256, 0);
						break;
					}

					cout << "Ŭ���̾�Ʈ [" << client->getClientID() << "]: " << roomInt << "�� ������ ����" << endl;

					// �ش� ������� �� ���� ���� ����
					Client *newClient = new Client(*client);
					newClient->setRoomID(roomInt);
					connections[i] = *newClient;

					// �濡 �����ߴٰ� �޼��� ����
					ZeroMemory(sent, 256);
					sprintf(sent, "%s", "[Enter]");
					send(client->getClientSocket(), sent, 256, 0);

					// ������ �̹� �濡 �� ���� �� ���� ����
					if (clientCount == 1) playClient(roomInt);
				}
			}
		}
		else if (receivedString.find("[Put]") != -1) {
			// �޼����� ���� Ŭ���̾�Ʈ ���� �ޱ�
			string data = tokens[1];
			vector<string> dataTokens = getTokens(data, ',');
			int roomID = atoi(dataTokens[0].c_str());
			int x = atoi(dataTokens[1].c_str());
			int y = atoi(dataTokens[2].c_str());

			cout << "[Put]: " << x << ", " << y << endl;

			// ����ڰ� ���� ���� ��ġ ����
			putClient(roomID, x, y);
		}
		else if (receivedString.find("[Play]") != -1) {
			// �޼����� ���� Ŭ���̾�Ʈ ���� �ޱ�
			string roomID = tokens[1];
			int roomInt = atoi(roomID.c_str());

			cout << "[Play]: " << roomInt << endl;
			// ���� ����
			playClient(roomInt);
		}
	}
}

int main() {
	WSAStartup(MAKEWORD(2, 2), &wsaData);
	serverSocket = socket(AF_INET, SOCK_STREAM, NULL);

	serverAddress.sin_family = AF_INET;
	serverAddress.sin_addr.s_addr = inet_addr("127.0.0.1");
	serverAddress.sin_port = htons(9876);

	cout << "[ C++  ���� ���� ���� ���� ]" << endl;
	bind(serverSocket, (SOCKADDR*)&serverAddress, sizeof(serverAddress));
	listen(serverSocket, 32);

	int addressLength = sizeof(serverAddress);

	while (1) {
		SOCKET clientSocket = socket(AF_INET, SOCK_STREAM, NULL);

		if (clientSocket = accept(serverSocket, (SOCKADDR*)&serverAddress, &addressLength)) {
			Client *client = new Client(nextID, clientSocket);
			cout << "[ ���ο� ����� ���� ]" << endl;
			CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)ServerThread, (LPVOID)client, NULL, NULL);
			connections.push_back(*client);
			nextID++;
		}

		Sleep(100);
	}
}
