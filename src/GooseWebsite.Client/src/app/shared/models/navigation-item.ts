// Gives the shared header a stable navigation contract across layouts.
export interface NavigationItem {
  label: string;
  id: string;
  // A page of its own: the link navigates there instead of scrolling to the section with this id.
  route?: string;
}
