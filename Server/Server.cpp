#include "Server.h"

SOCKET Server::serverSocket;
WSAData Server::wsaData;
SOCKADDR_IN Server::serverAddress;
int Server::nextID;
vector<Client*> Server::connections;
Util Server::util;

void Server::enterClient(Client *client) {
	char *sent = new char[256];
	ZeroMemory(sent, 256);
	sprintf(sent, "%s", "[Enter]");
	send(client->getClientSocket(), sent, 256, 0);
}

void Server::fullClient(Client *client) {
	char *sent = new char[256];
	ZeroMemory(sent, 256);
	sprintf(sent, "%s", "[Full]");
	send(client->getClientSocket(), sent, 256, 0);
}

void Server::playClient(int roomID) {
	char *sent = new char[256];
	bool black = true;

	for (int i = 0; i < connections.size(); i++) {
		if (connections[i]->getRoomID() == roomID) {
			ZeroMemory(sent, 256);

			if (black) {
				sprintf(sent, "%s", "[Play]Black");
				black = false;
			}
			else {
				sprintf(sent, "%s", "[Play]White");
			}

			send(connections[i]->getClientSocket(), sent, 256, 0);
		}
	}
}

void Server::exitClient(int roomID) {
	char *sent = new char[256];

	for (int i = 0; i < connections.size(); i++) {
		if (connections[i]->getRoomID() == roomID) {
			ZeroMemory(sent, 256);
			sprintf(sent, "%s", "[Exit]");
			send(connections[i]->getClientSocket(), sent, 256, 0);
		}
	}
}

void Server::putClient(int roomID, int x, int y) {
	char *sent = new char[256];

	for (int i = 0; i < connections.size(); i++) {
		if (connections[i]->getRoomID() == roomID) {
			ZeroMemory(sent, 256);
			string data = "[Put]" + to_string(x) + "," + to_string(y);
			sprintf(sent, "%s", data.c_str());
			send(connections[i]->getClientSocket(), sent, 256, 0);
		}
	}
}

int Server::clientCountInRoom(int roomID) {
	int count = 0;

	for (int i = 0; i < connections.size(); i++) {
		if (connections[i]->getRoomID() == roomID) {
			count++;
		}
	}

	return count;
}

void Server::start() {
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
			connections.push_back(client);
			nextID++;
		}

		Sleep(100);
	}
}

void Server::ServerThread(Client *client) {
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
				if (connections[i]->getClientID() == client->getClientID()) {
					// �ٸ� ����ڿ� ���� ���̴� ����� ���� ���
					int roomID = connections[i]->getRoomID();
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
		vector<string> tokens = util.getTokens(receivedString, ']');

		if (receivedString.find("[Enter]") != -1) {
			string roomID = tokens[1];
			int roomInt = atoi(roomID.c_str());
			int clientCount = clientCountInRoom(roomInt);

			// �޼����� ���� Ŭ���̾�Ʈ�� ã��
			for (int i = 0; i < connections.size(); i++) {
				if (connections[i]->getClientID() == client->getClientID()) {
					// ���� �� á�� ��
					if (clientCount >= 2) {
						fullClient(client);
						break;
					}

					cout << "Ŭ���̾�Ʈ [" << client->getClientID() << "]: " << roomInt << "�� ������ ����" << endl;

					// �ش� ������� �� ���� ���� ����
					client->setRoomID(roomInt);

					// �濡 �����ߴٰ� �޼��� ����
					enterClient(client);

					// ������ �̹� �濡 �� ���� �� ���� ����
					if (clientCount == 1) playClient(roomInt);
				}
			}
		}
		else if (receivedString.find("[Put]") != -1) {
			// �޼����� ���� Ŭ���̾�Ʈ ���� �ޱ�
			string data = tokens[1];
			vector<string> dataTokens = util.getTokens(data, ',');
			int x = atoi(dataTokens[0].c_str());
			int y = atoi(dataTokens[1].c_str());

			// ����ڰ� ���� ���� ��ġ ����
			putClient(client->getRoomID(), x, y);
		}
		else if (receivedString.find("[Play]") != -1) {
			// ���� ����
			playClient(client->getRoomID());
		}
	}
}