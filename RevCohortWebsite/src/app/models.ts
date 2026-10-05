export const TRACKS = ['React', 'Angular'];

export interface Me {
  discordId: number;
  username: string;
  firstName: string;
  lastName: string;
  profileColor: string;
  role: number;
  isAdmin: boolean;
  track: number;
  profileLink: string;
}

export interface Profile {
  discordId: string;
  username: string;
  track: number;
  profileColor: string;
  profileLink: string;
}

export interface TopicSection {
  heading: string;
  points: string[];
}

export interface Topic {
  slug: string;
  name: string;
  sections: TopicSection[];
}

export interface QuizQuestion {
  question: string;
  type: string;
  answers: string[];
  correctAnswer: string;
}
