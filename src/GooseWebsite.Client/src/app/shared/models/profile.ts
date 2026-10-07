// Keeps public profile content in one shared shape for all page sections.
export interface Profile {
  name: string;
  initials: string;
  title: string;
  positioning: string;
  bio: string;
  professionalSummary: string;
  location: string;
  email: string;
  links: {
    github: string;
    linkedin: string;
  };
}

export const profile: Profile = {
  name: 'Gustavo Couto Vanin',
  initials: 'GV',
  title: 'Application Security Engineer',
  positioning: 'Engineer interested in people and technology.',
  bio: 'I am a curious person who likes learning across sports, gaming, and culture. I will explore those interests more in the stories section.',
  professionalSummary: 'I build scalable security programs, automate complex workflows, and partner directly with engineering teams to ship secure software fast. I combine strong C#/Python development skills with deep experience in SDL, threat modeling, SAST/SCA, and vulnerability management. My work reduces risk, accelerates developer velocity, and uses AI-driven automation to turn security from a bottleneck into a force multiplier.',
  location: 'Southern California',
  email: 'gustavo.vanin@hotmail.com',
  links: {
    github: 'https://github.com/GucciVanin',
    linkedin: 'https://www.linkedin.com/in/gustavovanin/',
  },
};