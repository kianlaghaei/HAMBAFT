import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { expect, it } from 'vitest'
import { TeamGuard } from '../routes/guards'

it('protects Team routes and sends unpaired visitors to pairing', () => {
  render(<MemoryRouter initialEntries={['/team/story']}><Routes><Route element={<TeamGuard />}><Route path="/team/story" element={<div>راز تیم</div>} /></Route><Route path="/pair" element={<div>صفحه جفت‌شدن</div>} /></Routes></MemoryRouter>)
  expect(screen.getByText('صفحه جفت‌شدن')).toBeInTheDocument()
  expect(screen.queryByText('راز تیم')).not.toBeInTheDocument()
})
