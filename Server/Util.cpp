#include "Util.h"

vector<string> Util::getTokens(string input, char delimieter) {
	vector<string> tokens;
	istringstream f(input);
	string s;

	while (getline(f, s, delimieter)) {
		tokens.push_back(s);
	}

	return tokens;
}